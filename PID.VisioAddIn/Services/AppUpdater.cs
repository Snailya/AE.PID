using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using AE.PID.Tools;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using Splat;

namespace AE.PID.Services;

/// <summary>
///     This class handles app update related event, such as app version check and installer persist.
///     Also, it provides a trigger to allow user to invoke update manually.
/// </summary>
public class AppUpdater : IEnableLogger
{
    private readonly CompositeDisposable _cleanUp = new();
    private readonly HttpClient _client;
    private readonly BehaviorSubject<ReleaseInfo> _updateAvailableTrigger = new(null);

    public AppUpdater(HttpClient client, ConfigurationService configuration)
    {
        _client = client;

        // automatically check update by interval if it not meets the user disabled period
        configuration.WhenAnyValue(x => x.AppCheckInterval)
            .Select(Observable.Interval)
            .Switch()
            .Merge(Observable
                .Return<
                    long>(-1)) // add an immediate value as the interval method emits only after the interval collapse.
            // ignore if it is not till the next check time
            .Where(_ => DateTime.Now > configuration.AppNextTime)
            .Do(_ => this.Log().Info("App update started. {Initiated by: Auto-Run}"))
            .SelectMany(_ => CheckUpdateAsync())
            .Do(_ => { configuration.AppNextTime = DateTime.Now + configuration.AppCheckInterval; })
            .Subscribe(_ => { })
            .DisposeWith(_cleanUp);

        // whenever an update is available, it triggers a subject, so that we could ask user for permission
        _updateAvailableTrigger
            .WhereNotNull()
            // switch to the main thread to display ui
            .ObserveOn(WindowManager.Dispatcher!)
            .Select(info =>
            {
                var messageBoxText = "发现新版本。" +
                                     Environment.NewLine +
                                     info.ReleaseNotes +
                                     Environment.NewLine;
                return WindowManager.ShowDialog(messageBoxText);
            })
            .ObserveOn(ThisAddIn.Dispatcher!)
            // if a user chooses to update, download the installer and invoke update
            .Where(result => result is MessageBoxResult.Yes or MessageBoxResult.OK)
            .SelectMany(_ => DownloadUpdateAsync())
            .Subscribe(InstallUpdate)
            .DisposeWith(_cleanUp);
    }

    public async Task<bool> CheckUpdateAsync()
    {
        try
        {
#if DEBUG
            using var response =
                await _client.GetAsync(
                    "check-version?version=0.0.0.0");
#else
            using var response =
                await _client.GetAsync(
                    $"check-version?version={FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}");
#endif
            if (!response.IsSuccessStatusCode) return false;

            var responseBody = await response.Content.ReadAsStringAsync();
            var jObject = JObject.Parse(responseBody);

            var isUpdateAvailable = (bool)jObject["isUpdateAvailable"]!;

            if (!isUpdateAvailable) return false;

            var versionToken = jObject["latestVersion"]!;

            // whenever there is a valid update, trigger the update process
            _updateAvailableTrigger.OnNext(new ReleaseInfo
            {
                Version = (string)versionToken["version"]!,
                ReleaseNotes = (string)versionToken["releaseNotes"]!
            });

            this.Log().Info($"New app version detected: {versionToken["version"]}");

            return true;
        }
        catch (HttpRequestException httpRequestException)
        {
            this.Log().Error(httpRequestException,
                "Failed to check update from server. Firstly, check if you are able to ping to the api. If not, connect administrator.");
            throw;
        }
        catch (KeyNotFoundException keyNotFoundException)
        {
            this.Log().Error(keyNotFoundException,
                "Some of the keys [isUpdateAvailable, latestVersion, downloadUrl, releaseNotes] not found in the response. Please check if the api response body is out of time.");
            throw;
        }
    }

    /// <summary>
    ///     Download the installer zip and persist in a local path.
    /// </summary>
    /// <returns>The path of the zip installer.</returns>
    private async Task<string> DownloadUpdateAsync()
    {
        try
        {
            using var response = await _client.GetAsync("download/0");
            response.EnsureSuccessStatusCode();

            var fileName = Path.GetFileName(
                GetFilenameFromContentDisposition(response.Content.Headers.ContentDisposition.ToString()) ??
                Path.GetTempFileName());

            var filePath = Path.GetFullPath(Path.Combine(Constants.TmpFolder, fileName));
            if (!File.Exists(filePath))
            {
                // Otherwise, get the content as a stream
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = File.Create(filePath);
                await contentStream.CopyToAsync(fileStream);
            }

            this.Log().Info($"New version of app cached at {filePath}.");

            return filePath;
        }
        catch (HttpRequestException httpRequestException)
        {
            this.Log().Error(httpRequestException,
                "Failed to get installer zip from server. Firstly, check if you are able to ping to the api. If not, connect administrator.");
            throw;
        }
        catch (DirectoryNotFoundException directoryNotFoundException)
        {
            this.Log().Error(directoryNotFoundException,
                "Failed to cache installer to local storage. Please check if the directory exist, if not create it manully, or it should be created next time app starts.");
            throw;
        }
    }

    /// <summary>
    ///     Get the filename from content disposition from a response.
    /// </summary>
    /// <param name="contentDisposition"></param>
    /// <returns></returns>
    private static string? GetFilenameFromContentDisposition(string contentDisposition)
    {
        // Split the header value by semicolons
        var parts = contentDisposition.Split(';');

        // Find the part that starts with "filename="
        return (from part in parts
            where part.Trim().StartsWith("filename=")
            select part.Substring(part.IndexOf('=') + 1).Trim(' ', '"')).FirstOrDefault();
    }

    /// <summary>
    ///     Execute .msi file in a new process. Need restart Visio to apply changes.
    /// </summary>
    /// <param name="filePath"></param>
    private void InstallUpdate(string filePath)
    {
        try
        {
            var startInfo = BuildProcessInfo(filePath) ??
                            throw new InvalidProgramException("The file specified is not a valid installer.");
            using var process = new Process();
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            this.Log().Error(ex, "Failed to execute installer.");
        }
    }

    private ProcessStartInfo? BuildProcessInfo(string filePath)
    {
        var extension = Path.GetExtension(filePath);

        return extension switch
        {
            ".msi" => new ProcessStartInfo
            {
                FileName = "msiexec",
                Arguments = $"/i \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            ".exe" => new ProcessStartInfo
            {
                FileName = filePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            _ => null
        };
    }


    /// <summary>
    ///     Find the WinRar.exe by regedit and extract .rar file using WinRar in new process.
    /// </summary>
    /// <param name="sourceArchiveFileName"></param>
    /// <param name="destinationDirectoryName"></param>
    private void ExtractAndOverWriteRarFile(string sourceArchiveFileName, string destinationDirectoryName)
    {
        try
        {
            Directory.CreateDirectory(destinationDirectoryName);

            const string registryKey = @"SOFTWARE\WinRAR";

            // Attempt to open the WinRAR registry key
            using var key = Registry.LocalMachine.OpenSubKey(registryKey);
            // Retrieve the InstallPath value from the registry
            var installPath = key?.GetValue("exe64");

            var startInfo = new ProcessStartInfo
            {
                FileName = $"{installPath}",
                Arguments =
                    $"x -o+ \"{sourceArchiveFileName}\" \"{destinationDirectoryName}\"", // -o+ means overwrite all
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }
        catch (Exception e)
        {
            this.Log().Error(e, "Failed to extract rar file.");
            throw;
        }
    }

    private class ReleaseInfo
    {
        public string Version { get; set; } = string.Empty;

        /// <summary>
        ///     The release information about the latest version.
        /// </summary>
        public string ReleaseNotes { get; set; } = string.Empty;
    }
}