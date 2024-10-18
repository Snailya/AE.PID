using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using AE.PID.Visio.Core.Exceptions;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using Refit;
using Splat;

namespace AE.PID.Visio.Shared.Services;

public class AppUpdateService(IApiFactory<IAppApi> apiFactory)
    : IAppUpdateService, IEnableLogger
{
    private readonly string _folder = Path.Combine(Assembly.GetExecutingAssembly().Location, "tmp");

    //<inheritdoc />
    public async Task<PendingAppUpdate?> CheckUpdateAsync(string version)
    {
        try
        {
            // get the lasted application info from the server
            var app = await apiFactory.Api.GetCurrentApp();

            // if the server version is no larger thant the local one, treat as no update
            if (new Version(app.Version) <= new Version(version))
            {
                this.Log().Info("No new app version available.");
                return null;
            }

            this.Log().Info($"New app version available, the version is {app.Version}.");

            return new PendingAppUpdate
            {
                Version = app.Version,
                ReleaseNotes = app.ReleaseNotes,
                DownloadUrl = app.DownloadUrl
            };
        }
        catch (ApiException apiException) when (apiException.StatusCode is HttpStatusCode.NoContent)
        {
            this.Log().Info("No new app version available.");
            return null;
        }
        catch (Exception e)
        {
            this.Log().Error(e, $"Params: [{nameof(version)}: {version}]");
            throw new NetworkNotValidException();
        }
    }

    //<inheritdoc />
    public Task InstallAsync(string executablePath)
    {
        try
        {
            if (string.IsNullOrEmpty(executablePath)) return Task.CompletedTask;

            var startInfo = BuildProcessInfo(executablePath);
            using var process = new Process();
            process.StartInfo = startInfo;

            this.Log().Info(
                $"Calling installer at {executablePath}, please follow the wizard to install the new version and restart Visio after done.");

            process.Start();
            process.WaitForExit();

            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            this.Log().Error(e, $"Params: [{nameof(executablePath)}: {executablePath}]");
            return Task.FromException(e);
        }
    }

    //<inheritdoc />
    public async Task<string> DownloadAsync(string downloadUrl)
    {
        try
        {
            this.Log().Info($"Starting download installer from {downloadUrl}...");

            using var response = await apiFactory.HttpClient.GetAsync(downloadUrl);

            var fileName = Path.GetFileName(
                GetFilenameFromContentDisposition(response.Content.Headers.ContentDisposition.ToString()) ??
                Path.GetTempFileName());

            var filePath = Path.GetFullPath(Path.Combine(_folder, fileName));

            // if it is not downloaded yet
            if (File.Exists(filePath)) return filePath;

            EnsureDirectoryCreated();

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = File.Create(filePath);
            await contentStream.CopyToAsync(fileStream);

            this.Log().Info($"The installer has been saved at {filePath}");

            return filePath;
        }
        catch (HttpRequestException e)
        {
            this.Log().Error(e, $"Params: [{nameof(downloadUrl)}: {downloadUrl}]");
            throw new NetworkNotValidException();
        }
    }

    private void EnsureDirectoryCreated()
    {
        if (!Directory.Exists(_folder)) Directory.CreateDirectory(_folder);
    }

    private static string? GetFilenameFromContentDisposition(string contentDisposition)
    {
        // Split the header value by semicolons
        var parts = contentDisposition.Split(';');

        // Find the part that starts with "filename="
        return (from part in parts
            where part.Trim().StartsWith("filename=")
            select part.Substring(part.IndexOf('=') + 1).Trim(' ', '"')).FirstOrDefault();
    }

    private static ProcessStartInfo BuildProcessInfo(string filePath)
    {
        var extension = Path.GetExtension(filePath);

        return extension switch
        {
            ".msi" => new ProcessStartInfo
            {
                FileName = "msiexec",
                Arguments = $"/i \"{filePath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            },
            ".exe" => new ProcessStartInfo
            {
                FileName = filePath,
                CreateNoWindow = true,
                // ensure the administrator privilege as the replacement in the Local folder might return access deny code 5 
                UseShellExecute = true,
                Verb = "runas"
            },
            _ => throw new UnsupportedFileExtensionException(extension)
        };
    }
}