using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using NLog;
using ReactiveUI;

namespace AE.PID.Controllers.Services;

/// <summary>
///     This class handles app update related event, such as app version check and installer persist.
///     Also, it provide a trigger to allow user to invoke update manually.
/// </summary>
public class AppUpdater
{
    private readonly CompositeDisposable _cleanUp = new();
    private readonly HttpClient _client;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly BehaviorSubject<ReleaseInfo?> _updateAvailableTrigger = new(null);

    public AppUpdater(HttpClient client, ConfigurationService configuration)
    {
        _client = client;

        // automatically check update by interval if it not meet the user disabled period
        configuration.WhenAnyValue(x => x.AppCheckInterval)
            .Select(Observable.Interval)
            .Switch()
            .Merge(Observable
                .Return<
                    long>(-1)) // add a immediately value as the interval method emits only after the interval collapse.
            // ignore if it not till the next check time
            .Where(_ => DateTime.Now > configuration.AppNextTime)
            .Do(_ => _logger.Info("App Update started. {Initiated by: Auto-Run}"))
            // switch to background thread
            .ObserveOn(ThreadPoolScheduler.Instance)
            .SelectMany(x => CheckUpdateAsync())
            .Do(_ => { configuration.AppNextTime = DateTime.Now + configuration.AppCheckInterval; })
            .Subscribe(v => { })
            .DisposeWith(_cleanUp);

        // whenever a update is available, it triggers a subject, so that we could ask user for permission
        _updateAvailableTrigger
            .WhereNotNull()
            // switch to main thread to display ui
            .ObserveOn(Globals.ThisAddIn.SynchronizationContext)
            .Select(AskForUpdate)
            // if user choose to update, download the installer and invoke update
            .Where(x => x)
            // switch back to thread pool
            .ObserveOn(ThreadPoolScheduler.Instance)
            .SelectMany(x => DownloadUpdateAsync())
            .Subscribe(InstallUpdate)
            .DisposeWith(_cleanUp);
    }

    public async Task<bool> CheckUpdateAsync()
    {
        try
        {
            using var response =
                await _client.GetAsync(
                    $"check-version?version={FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}");
            response.EnsureSuccessStatusCode();

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

            return true;
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.Error(httpRequestException,
                "Failed to check update from server. Firstly, check if you are able to ping to the api. If not, connect administrator.");
            throw;
        }
        catch (KeyNotFoundException keyNotFoundException)
        {
            _logger.Error(keyNotFoundException,
                "Some of the keys [isUpdateAvailable, latestVersion, downloadUrl, releaseNotes] not found in the response. Please check if the api response body is out of time.");
            throw;
        }
    }


    /// <summary>
    ///     Prompt user the get update result to let user decide whether to perform a update right now.
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    private static bool AskForUpdate(ReleaseInfo info)
    {
        // todo: refactor using wpf
        var description = "发现新版本。" +
                          Environment.NewLine +
                          info.ReleaseNotes +
                          Environment.NewLine;
        var result = DialogResult.Yes == ThisAddIn.AskForUpdate(description);
        return result;
    }

    /// <summary>
    ///     Download the installer zip and persist in a local path.
    /// </summary>
    /// <returns>The path of the zip installer.</returns>
    private async Task<string> DownloadUpdateAsync()
    {
        try
        {
            using var response = await _client.GetAsync("download");
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

            _logger.Info($"New version of app cached at {filePath}.");

            return filePath;
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.Error(httpRequestException,
                "Failed to get installer zip from server. Firstly, check if you are able to ping to the api. If not, connect administrator.");
            throw;
        }
        catch (DirectoryNotFoundException directoryNotFoundException)
        {
            _logger.Error(directoryNotFoundException,
                "Failed to cache installer to local storage. Please check if the directory exist, if not create it manully, or it should be created next time app starts.");
            throw;
        }
    }

    /// <summary>
    ///     Get the filename from content disposition from an response.
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
        string msiPath;
        if (Path.GetExtension(filePath) == ".rar")
        {
            var destination = Path.Combine(Constants.TmpFolder, Path.GetFileNameWithoutExtension(filePath));
            ExtractAndOverWriteRarFile(filePath, destination);
            var files = Directory.GetFiles(destination);
            msiPath = files.Single(x => Path.GetExtension(x) == ".msi");
        }
        else
        {
            msiPath = filePath;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "msiexec",
                Arguments = $"/i \"{msiPath}\"",
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
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to execute installer.");
        }
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
                    $"x -o+ \"{sourceArchiveFileName}\" \"{destinationDirectoryName}\"", // -o+ means overwirte all
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
            _logger.Error(e, "Failed to extract rar file.");
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