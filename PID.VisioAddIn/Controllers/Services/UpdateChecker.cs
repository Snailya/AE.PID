using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using AE.PID.Models;
using AE.PID.Views;
using NLog;
using Logger = NLog.Logger;

namespace AE.PID.Controllers.Services;

public class UpdateChecker
{
    private readonly string _baseUrl;
    private readonly HttpClient _clinet;
    private readonly Configuration _config;

    private readonly Logger _logger;

    private Window _versionUpdatePromptWindow;

    public UpdateChecker()
    {
        _logger = LogManager.GetCurrentClassLogger();
        _clinet = Globals.ThisAddIn.GetHttpClient();
        _config = Globals.ThisAddIn.GetCurrentConfiguration();
        _baseUrl = _config.Api;
    }

    public event EventHandler CheckUpdateCompleted;

    /// <summary>
    ///     Check for valid update and prompts a messagebox to let user decide whether to update, if true do update.
    /// </summary>
    public IObservable<bool> CheckForUpdate()
    {
        return CheckForAvailableUpdateAsync().ToObservable().Where(x => x.IsUpdateAvailable)
            .Select(x => (x.DownloadUrl, x.ReleaseNotes))
            .SelectMany(data => CacheInstallerAsync(data.DownloadUrl).ToObservable().Zip(
                    AskToUpdateAsync(data.ReleaseNotes).ToObservable(),
                    (path, userDecision) => (path, userDecision)
                )
            ).Where(result => !string.IsNullOrEmpty(result.path) && result.userDecision)
            .Select(result => DoUpdate(result.path));
    }

    private bool DoUpdate(string installer)
    {
        try
        {
            Process.Start("explorer.exe", $"/select, \"{installer}\"");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogUsefulException(ex);
        }

        return false;
    }

    private async Task<AppCheckVersionResult> CheckForAvailableUpdateAsync()
    {
        try
        {
            var local = _config.Version;

            using var response = await _clinet.GetAsync(_baseUrl + $"/check-version?version={local}");
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
        catch (Exception ex)
        {
            _logger.LogUsefulException(ex);
            throw;
        }
    }

    private async Task<string> CacheInstallerAsync(string downloadUrl)
    {
        try
        {
            using var response = await _clinet.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();

            // Get the content as a stream
            using var contentStream = await response.Content.ReadAsStreamAsync();

            // Save the stream content to a file
            var filePath = Path.GetTempFileName();
            using var fileStream = File.Create(filePath);
            await contentStream.CopyToAsync(fileStream);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogUsefulException(ex);
            throw;
        }
    }

    private static Task<bool> AskToUpdateAsync(string description)
    {
        return Task.FromResult(MessageBoxResult.Yes ==
                               MessageBox.Show(description + Environment.NewLine + "现在更新?", "更新",
                                   MessageBoxButton.YesNo));
    }


    /// <summary>
    ///     Opens a window to show the update's information
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void AskToUpdate(IEnumerable<JsonElement> stencilObjects)
    {
        if (_versionUpdatePromptWindow == null)
        {
            _versionUpdatePromptWindow = new Window
            {
                Title = "更新",
                Height = 240,
                Width = 320,
                MinHeight = 240,
                MinWidth = 320,
                Content = new VersionUpdatePromptView(stencilObjects)
            };

            _versionUpdatePromptWindow.Closed += VersionUpdatePromptWindow_Closed;
            _versionUpdatePromptWindow.Show();
        }

        _versionUpdatePromptWindow.Activate();
    }

    private void VersionUpdatePromptWindow_Closed(object sender, EventArgs e)
    {
        _versionUpdatePromptWindow = null;
    }


    public void CloseVersionUpdatePromptWindow()
    {
        if (_versionUpdatePromptWindow != null)
        {
            _versionUpdatePromptWindow.Close();
            _versionUpdatePromptWindow = null;
        }
    }
}

public class AppCheckVersionResult
{
    public bool IsUpdateAvailable { get; set; }
    public string DownloadUrl { get; set; }
    public string ReleaseNotes { get; set; }
}