using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using AE.PID.Dtos;
using AE.PID.Models;
using AE.PID.Properties;
using AE.PID.Tools;
using Microsoft.Office.Interop.Visio;
using Newtonsoft.Json;
using ReactiveUI;
using Splat;
using Path = System.IO.Path;

namespace AE.PID.Services;

/// <summary>
///     Compare local library configuration with the server, and download newest if exist.
/// </summary>
public class LibraryUpdater : IEnableLogger
{
    private readonly ApiClient _client;
    private readonly ConfigurationService _configuration;

    public LibraryUpdater(ApiClient? client = null, ConfigurationService? configuration = null)
    {
        _client = client ?? Locator.Current.GetService<ApiClient>()!;
        _configuration = configuration ?? Locator.Current.GetService<ConfigurationService>()!;

        var autoCheckObservable = configuration
            .WhenAnyValue(x => x.LibraryCheckInterval)
            .Select(Observable.Interval)
            .Switch()
            .Merge(Observable.Return<long>(-1))
            .Where(_ =>
                DateTime.Now > _configuration.LibraryNextTime)
            .Select(_ => Unit.Default)
            .Do(_ => this.Log().Info("Library Update started. {Initiated by: Auto-Run}"));

        var serverChangeObservable = _configuration
            .WhenAnyValue(x => x.Server)
            .Select(_ => Unit.Default)
            .Do(_ => this.Log().Info("Library update started. {Initiated by: Server-Change}"));

        var userCheckObservable =
            ManuallyInvokeTrigger
                .Do(_ => this.Log().Info("Library Update started. {Initiated by: User}"));

        autoCheckObservable
            .Merge(userCheckObservable)
            .Merge(serverChangeObservable)
            .SelectMany(_ => CheckLibraryUpdates())
            .Do(_ => { _configuration.LibraryNextTime = DateTime.Now + _configuration.LibraryCheckInterval; })
            .Where(x => x.Any())
            .SelectMany(UpdateLibrariesAsync)
            .Subscribe(_configuration.UpdateLibraries,
                exception => { this.Log().Error(exception, "Library update failed."); });
    }

    public Subject<Unit> ManuallyInvokeTrigger { get; } = new();

    /// <summary>
    ///     Get libraries from server
    /// </summary>
    /// <returns></returns>
    public async Task<List<LibraryDto>> GetLibraryInfos()
    {
        this.Log().Info("Try getting library infos from server.");

        try
        {
            var response = await _client.GetStringAsync(LibraryInfoApi);

            // Members that return a sequence should never return null. Return an empty sequence instead
            return JsonConvert.DeserializeObject<IEnumerable<LibraryDto>>(response)?.ToList() ?? [];
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
        {
            this.Log().Error(ex,
                $"Failed to get library infos from {LibraryInfoApi}. Firstly, check if the server address is correct. Then check if it is connectable.");
        }

        return [];
    }

    /// <summary>
    ///     Filter out different libraries.
    /// </summary>
    /// <returns></returns>
    private async Task<IEnumerable<LibraryDto>> CheckLibraryUpdates()
    {
        var libraryInfos = await GetLibraryInfos();

        return libraryInfos.Where(i =>
            _configuration.Libraries.Lookup(i.Id).HasValue == false ||
            _configuration.Libraries.Lookup(i.Id).Value.Version != i.Version);
    }

    /// <summary>
    ///     Download the cheat sheet. The cheat sheet is used by Document Master Update Tool.
    /// </summary>
    private async Task DownloadCheatSheet()
    {
        try
        {
            var responseString = await _client.GetStringAsync(CheatSheetApi);

            if (string.IsNullOrEmpty(responseString)) return;
            var responseResult = JsonConvert.DeserializeObject<IEnumerable<DetailedLibraryItemDto>>(responseString);
            Debug.Assert(responseResult != null);

            using var writer = new StreamWriter(Constants.LibraryCheatSheetPath);
            await writer.WriteLineAsync(responseString);
        }
        catch (Exception e)
        {
            this.Log().Error(e, "Failed to download cheatsheet from server.");
        }
    }

    /// <summary>
    ///     Update the library files on local machine.
    /// </summary>
    /// <param name="libraryDtos"></param>
    /// <returns></returns>
    private async Task<IEnumerable<ReactiveLibrary>> UpdateLibrariesAsync(IEnumerable<LibraryDto> libraryDtos)
    {
        try
        {
            var libraryToUpdates = libraryDtos as LibraryDto[] ?? libraryDtos.ToArray();

            foreach (var libraryToUpdate in libraryToUpdates)
            {
                using var response = await _client.GetAsync(libraryToUpdate.DownloadUrl);
                using var contentStream = await response.Content.ReadAsStreamAsync();

                var fullName = Path.ChangeExtension(
                    Path.Combine(Constants.LibraryFolder, libraryToUpdate.Name),
                    "vssx");

                // close the origin file if it is opened
                var isOpened = await ThisAddIn.Dispatcher!.InvokeAsync(() =>
                {
                    var currentDocument = Globals.ThisAddIn.Application.Documents.OfType<Document>()
                        .SingleOrDefault(x => x.FullName == fullName);
                    if (currentDocument == null) return false;

                    currentDocument.Close();
                    return true;
                });

                // do overwrite
                using (var fileStream = File.Open(fullName, FileMode.Create, FileAccess.Write))
                {
                    await contentStream.CopyToAsync(fileStream);
                }

                // restore open status
                if (isOpened)
                    ThisAddIn.Dispatcher.Invoke(() =>
                    {
                        Globals.ThisAddIn.Application.Documents.OpenEx(fullName,
                            (short)VisOpenSaveArgs.visOpenDocked);
                    });
            }

            await DownloadCheatSheet();
            return libraryToUpdates.Select(ReactiveLibrary.FromLibraryDto);
        }
        catch (IOException ioException)
        {
            this.Log().Error(ioException,
                "Failed to overwrite library file. It might be used by other process, consider close it before retry.");
            WindowManager.ShowDialog(string.Format(Resources.MSG_write_file_failed_with_message, ioException.Message),
                MessageBoxButton.OK);
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
        {
            this.Log().Error(ex,
                "Failed to donwload library from server. Firstly, check if the server address is correct. Then check if it is connectable.");
            WindowManager.ShowDialog(
                string.Format(Resources.MSG_server_connect_failed_with_message, ex.Message),
                MessageBoxButton.OK);
        }

        return [];
    }

    #region Api

    private static string LibraryInfoApi => "libraries";

    private static string CheatSheetApi => "libraries/cheatsheet";

    #endregion
}