using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AE.PID.Models;
using NLog;

namespace AE.PID.Controllers.Services;

public abstract class AppUpdater
{
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

    public static void DoUpdate(string installerPath)
    {
        var logger = LogManager.GetCurrentClassLogger();

        logger.Info($"Open the explorer and select {installerPath}");
        Process.Start("explorer.exe", $"/select, \"{installerPath}\"");
    }
}