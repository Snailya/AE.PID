using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLog;
using Microsoft.Win32;
using ReactiveUI;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace AE.PID.Controllers.Services;

/// <summary>
/// This class handles app update related event, such as app version check and installer persist.
/// Also, it provide a trigger to allow user to invoke update manually.
/// </summary>
public abstract class AppUpdater
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static Subject<Unit> ManuallyInvokeTrigger { get; } = new();

    /// <summary>
    ///     Emit a value manually.
    /// </summary>
    public static void Invoke()
    {
        ManuallyInvokeTrigger.OnNext(Unit.Default);
    }

    /// <summary>
    /// Staring a background service to request the server on period to check if there is a valid update.
    /// If so, prompt a MessageBox to let user decide whether to update right now.
    /// As automatic update not implemented, only open the explorer window to let user know there's a update installer.
    /// This should called after configuration loaded.
    /// </summary>
    public static IDisposable Listen()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        Logger.Info($"App Update Service started. Current version: {version}");

        var userInvokeObservable = ManuallyInvokeTrigger
            .Throttle(TimeSpan.FromMilliseconds(300))
            .Select(_ => Constants.ManuallyInvokeMagicNumber)
            .Do(_ => Logger.Info($"App Update started. {{Initiated by: User}}"));

        var autoInvokeObservable = Globals.ThisAddIn.Configuration.CheckIntervalSubject // auto check
            .Select(Observable.Interval)
            .Switch()
            .Merge(Observable
                .Return<
                    long>(-1)) // add a immediately value as the interval method emits only after the interval collapse.
            .Where(_ => DateTime.Now >
                        Globals.ThisAddIn.Configuration.NextTime) // ignore if it not till the next check time
            .Do(_ => Logger.Info($"App Update started. {{Initiated by: Auto-Run}}"));

        return autoInvokeObservable
            .Merge(userInvokeObservable)
            .Subscribe(InvokeUpdate,
                ex => { Logger.Error(ex, $"App update listener ternimated accidently."); },
                () => { Logger.Error("App update listener should never complete."); });
    }

    /// <summary>
    /// Invoke a app update process.
    /// </summary>
    /// <param name="seed"></param>
    private static void InvokeUpdate(long seed)
    {
        Observable.Return(seed)
            .SubscribeOn(TaskPoolScheduler.Default)
            // check for valid update from server
            .SelectMany(value => Observable.FromAsync(GetUpdateAsync),
                (value, result) => new { InvokeType = value, Result = result })
            // notify user if user decision needs to decide whether to continue updating or not.
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(data => NotifyUserIfNeed(data.InvokeType, data.Result) ? data.Result.DownloadUrl : string.Empty)
            // continue updating
            .ObserveOn(TaskPoolScheduler.Default)
            .Where(x => !string.IsNullOrEmpty(x))
            .SelectMany(CacheAsync)
            .Subscribe(
                Install,
                ex =>
                {
                    switch (ex)
                    {
                        case HttpRequestException httpRequestException:
                            Logger.Error(httpRequestException,
                                "Failed to donwload library from server. Firstly, check if you are able to ping to the api. If not, connect administrator.");
                            ThisAddIn.Alert("无法连接至服务器，请检查网络。");
                            break;
                    }
                }
            );
    }

    /// <summary>
    /// Request the server with current version of the app to check if there's a available update.
    /// </summary>
    /// <returns>The check result, if there's an available version, return with the lasted version download url, otherwise with message.</returns>
    private static async Task<AppCheckVersionResult> GetUpdateAsync()
    {
        var configuration = Globals.ThisAddIn.Configuration;
        var client = Globals.ThisAddIn.HttpClient;
        var version = Assembly.GetExecutingAssembly().GetName().Version;

        try
        {
            using var response =
                await client.GetAsync(configuration.Api + $"/check-version?version={version}");
            response.EnsureSuccessStatusCode();

            // anytime there's a success response from check-version, the check time should update.
            configuration.NextTime = DateTime.Now + configuration.CheckInterval;
            configuration.Save();

            var responseBody = await response.Content.ReadAsStringAsync();

            var jObject = JObject.Parse(responseBody);

            var isUpdate = (bool)jObject["isUpdateAvailable"]!;
            var versionToken = jObject["latestVersion"]!;

            Logger.Info(isUpdate ? "New app version available." : "App is up to date.");

            if (isUpdate)
                return new AppCheckVersionResult
                {
                    IsUpdateAvailable = true,
                    DownloadUrl = (string)versionToken["downloadUrl"]!,
                    ReleaseNotes = (string)versionToken["releaseNotes"]!
                };

            return new AppCheckVersionResult { IsUpdateAvailable = false };
        }
        catch (HttpRequestException httpRequestException)
        {
            Logger.Error(httpRequestException,
                $"Failed to check update from server. Firstly, check if you are able to ping to the api. If not, connect administrator.");
            throw;
        }
        catch (KeyNotFoundException keyNotFoundException)
        {
            Logger.Error(keyNotFoundException,
                $"Some of the keys [isUpdateAvailable, latestVersion, downloadUrl, releaseNotes] not found in the response. Please check if the api response body is out of time.");
            throw;
        }
    }

    /// <summary>
    /// Prompt user the get update result to let user decide whether to perform a update right now.
    /// </summary>
    /// <param name="invokeType"></param>
    /// <param name="checkResult"></param>
    /// <returns></returns>
    private static bool NotifyUserIfNeed(long invokeType, AppCheckVersionResult checkResult)
    {
        if (!checkResult.IsUpdateAvailable)
        {
            // prompt a already updated version message if the check is invoked by user
            if (invokeType == Constants.ManuallyInvokeMagicNumber)
                ThisAddIn.Alert("这就是最新版本。");

            return false;
        }

        var description = "发现新版本。" +
                          Environment.NewLine +
                          checkResult.ReleaseNotes +
                          Environment.NewLine + Environment.NewLine +
                          "请在安装完成后重启Visio";
        var result = DialogResult.Yes == ThisAddIn.AskForUpdate(description);
        return result;
    }

    /// <summary>
    /// Download the installer zip and persist in a local path.
    /// </summary>
    /// <param name="downloadUrl"></param>
    /// <returns>The path of the zip installer.</returns>
    private static async Task<string> CacheAsync(string downloadUrl)
    {
        var client = Globals.ThisAddIn.HttpClient;

        // initialize the folder if not exist
        var installerFolder = Path.Combine(ThisAddIn.TmpFolder);
        if (!Directory.Exists(installerFolder)) Directory.CreateDirectory(installerFolder);

        try
        {
            using var response = await client.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();

            var fileName = Path.GetFileName(
                GetFilenameFromContentDisposition(response.Content.Headers.ContentDisposition.ToString()) ??
                Path.GetTempFileName());

            var filePath = Path.GetFullPath(Path.Combine(installerFolder, fileName));
            if (File.Exists(filePath)) return filePath; // already exist

            // Otherwise, get the content as a stream
            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = File.Create(filePath);
            await contentStream.CopyToAsync(fileStream);

            Logger.Info($"New version of app cached at {filePath}.");

            return filePath;
        }
        catch (HttpRequestException httpRequestException)
        {
            Logger.Error(httpRequestException,
                $"Failed to get installer zip from server. Firstly, check if you are able to ping to the api. If not, connect administrator.");
            throw;
        }
        catch (DirectoryNotFoundException directoryNotFoundException)
        {
            Logger.Error(directoryNotFoundException,
                $"Failed to cache installer to local storage. Please check if the directory exist, if not create it manully, or it should be created next time app starts.");
            throw;
        }
    }

    /// <summary>
    /// Get the filename from content disposition from an response.
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
    /// Execute .msi file in a new process. Need restart Visio to apply changes.
    /// </summary>
    /// <param name="source"></param>
    private static void Install(string source)
    {
        string msiPath;
        if (Path.GetExtension(source) == ".rar")
        {
            var destination = Path.Combine(ThisAddIn.TmpFolder, Path.GetFileNameWithoutExtension(source));
            ExtractAndOverWriteRarFile(source, destination);
            var files = Directory.GetFiles(destination);
            msiPath = files.Single(x => Path.GetExtension(x) == ".msi");
        }
        else
        {
            msiPath = source;
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
            Logger.Error(ex, "Failed to execute installer.");
        }
    }

    /// <summary>
    /// Find the WinRar.exe by regedit and extract .rar file using WinRar in new process.
    /// </summary>
    /// <param name="sourceArchiveFileName"></param>
    /// <param name="destinationDirectoryName"></param>
    private static void ExtractAndOverWriteRarFile(string sourceArchiveFileName, string destinationDirectoryName)
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
            Logger.Error(e, "Failed to extract rar file.");
            throw;
        }
    }

    private class AppCheckVersionResult
    {
        /// <summary>
        ///     Indicates whether a update is available at DownloadUrl.
        /// </summary>
        public bool IsUpdateAvailable { get; set; }

        /// <summary>
        ///     The request url to get the latest version app.
        /// </summary>
        public string DownloadUrl { get; set; }

        /// <summary>
        ///     The release information about the latest version.
        /// </summary>
        public string ReleaseNotes { get; set; }
    }
}