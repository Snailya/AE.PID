using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Threading.Tasks;
using AE.PID.Models;
using NLog;

namespace AE.PID.Controllers.Services;

/// <summary>
/// This class handles app update related event, such as app version check and installer persist.
/// Also, it provide a trigger to allow user to invoke update manually.
/// </summary>
public abstract class AppUpdater
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Trigger used for ui Button to invoke the update event.
    /// </summary>
    public static Subject<Unit> ManuallyInvokeTrigger { get; } = new();

    /// <summary>
    ///     Emit a value manually
    /// </summary>
    public static void Invoke()
    {
        ManuallyInvokeTrigger.OnNext(Unit.Default);
    }

    /// <summary>
    /// Request the server with current version of the app to check if there's a available update.
    /// </summary>
    /// <returns>The check result, if there's an available version, return with the lasted version download url, otherwise with message.</returns>
    public static async Task<AppCheckVersionResult> GetUpdateAsync()
    {
        var configuration = Globals.ThisAddIn.Configuration;
        var client = Globals.ThisAddIn.HttpClient;

        try
        {
            using var response =
                await client.GetAsync(configuration.Api + $"/check-version?version={configuration.Version}");
            response.EnsureSuccessStatusCode();

            // anytime there's a success response from check-version, the check time should update.
            configuration.NextTime = DateTime.Now + configuration.CheckInterval;
            Configuration.Save(configuration);

            var responseBody = await response.Content.ReadAsStringAsync();

            var jsonDocument = JsonDocument.Parse(responseBody);
            var root = jsonDocument.RootElement;

            var isUpdate = root.GetProperty("isUpdateAvailable").GetBoolean();
            if (isUpdate)
                return new AppCheckVersionResult
                {
                    IsUpdateAvailable = true,
                    DownloadUrl = root.GetProperty("latestVersion").GetProperty("downloadUrl").GetString(),
                    ReleaseNotes = root.GetProperty("latestVersion").GetProperty("releaseNotes").GetString()
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
    /// Download the installer zip and persist in a local path.
    /// </summary>
    /// <param name="downloadUrl"></param>
    /// <returns>The path of the zip installer.</returns>
    public static async Task<string> CacheAsync(string downloadUrl)
    {
        var client = Globals.ThisAddIn.HttpClient;

        // initialize the folder if not exist
        var installerFolder = Path.Combine(ThisAddIn.TmpFolder);
        if (!Directory.Exists(installerFolder)) Directory.CreateDirectory(installerFolder);

        try
        {
            using var response = await client.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();

            var filePath = Path.GetFullPath(Path.Combine(installerFolder,
                Path.GetFileName(
                    GetFilenameFromContentDisposition(response.Content.Headers.ContentDisposition.ToString()))));
            if (File.Exists(filePath)) return filePath; // already exist

            // Otherwise, get the content as a stream
            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = File.Create(filePath);
            await contentStream.CopyToAsync(fileStream);

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

    private static string GetFilenameFromContentDisposition(string contentDisposition)
    {
        // Split the header value by semicolons
        var parts = contentDisposition.Split(';');

        // Find the part that starts with "filename="
        return (from part in parts
            where part.Trim().StartsWith("filename=")
            select part.Substring(part.IndexOf('=') + 1).Trim(' ', '"')).FirstOrDefault();
    }

    /// <summary>
    /// Open the explorer.exe and select the installer exe.
    /// </summary>
    /// <param name="installerPath"></param>
    public static void PromptManuallyUpdate(string installerPath)
    {
        Logger.Info($"Open the explorer and select {installerPath}");
        Process.Start("explorer.exe", $"/select, \"{installerPath}\"");
    }
}