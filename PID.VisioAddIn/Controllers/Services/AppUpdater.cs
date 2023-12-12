using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AE.PID.Models;
using NLog;

namespace AE.PID.Controllers.Services;

/// <summary>
/// Check app version from server and get latest version installer.
/// </summary>
public abstract class AppUpdater
{
    /// <summary>
    /// Request the server with current version of the app to check if there's a available update.
    /// </summary>
    /// <returns>The check result, if there's an available version, return with the lasted version download url, otherwise with message.</returns>
    public static async Task<AppCheckVersionResult> GetUpdateAsync()
    {
        var configuration = Globals.ThisAddIn.Configuration;
        var client = Globals.ThisAddIn.HttpClient;

        using var response =
            await client.GetAsync(configuration.Api + $"/check-version?version={configuration.Version}");
        response.EnsureSuccessStatusCode();
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

    /// <summary>
    /// Download the installer zip and persist in a local path.
    /// </summary>
    /// <param name="downloadUrl"></param>
    /// <returns>The path of the zip installer.</returns>
    public static async Task<string> CacheAsync(string downloadUrl)
    {
        var client = Globals.ThisAddIn.HttpClient;

        using var response = await client.GetAsync(downloadUrl);
        response.EnsureSuccessStatusCode();

        // Get the content as a stream
        using var contentStream = await response.Content.ReadAsStreamAsync();

        // Save the stream content to a file
        var installerFolder = Path.Combine(Globals.ThisAddIn.DataFolder, "TEMP");
        if (!Directory.Exists(installerFolder)) Directory.CreateDirectory(installerFolder);

        var filePath = Path.Combine(installerFolder,
            Path.GetFileName(
                GetFilenameFromContentDisposition(response.Content.Headers.ContentDisposition.ToString())));
        using var fileStream = File.Create(filePath);
        await contentStream.CopyToAsync(fileStream);

        return Path.GetFullPath(filePath);
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
    public static void DoUpdate(string installerPath)
    {
        var logger = LogManager.GetCurrentClassLogger();

        logger.Info($"Open the explorer and select {installerPath}");
        Process.Start("explorer.exe", $"/select, \"{installerPath}\"");
    }
}