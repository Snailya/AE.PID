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
using Newtonsoft.Json;
using ReactiveUI;
using Splat;

namespace AE.PID.Services;

/// <summary>
///     Compare local library configuration with the server, and download newest if exist.
/// </summary>
public class LibraryUpdater : IEnableLogger
{
    private readonly HttpClient _client;
    private readonly ConfigurationService _configuration;

    public LibraryUpdater(HttpClient client, ConfigurationService configuration)
    {
        _client = client;
        _configuration = configuration;

        var autoCheckObservable = configuration
            .WhenAnyValue(x => x.LibraryCheckInterval)
            .Select(Observable.Interval)
            .Switch()
            .Merge(Observable.Return<long>(-1))
            .Where(_ =>
                DateTime.Now > configuration.LibraryNextTime)
            .Select(_ => Unit.Default)
            .Do(_ => this.Log().Info("Library Update started. {Initiated by: Auto-Run}"));

        var userCheckObservable =
            ManuallyInvokeTrigger
                .Do(_ => this.Log().Info("Library Update started. {Initiated by: User}"));

        autoCheckObservable.Merge(userCheckObservable)
            .SelectMany(_ => CheckLibraryUpdates())
            .Do(_ => { configuration.LibraryNextTime = DateTime.Now + configuration.LibraryCheckInterval; })
            .Where(x => x.Any())
            .SelectMany(UpdateLibrariesAsync)
            .Subscribe(configuration.UpdateLibraries);
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
            var requestUrl = "/libraries";
#if DEBUG
            requestUrl = "/libraries?involvePrerelease=true";
#endif
            var response = await _client.GetStringAsync(requestUrl);

            // Members that return a sequence should never return null. Return an empty sequence instead
            return JsonConvert.DeserializeObject<IEnumerable<LibraryDto>>(response)?.ToList() ?? [];
        }
        catch (HttpRequestException httpRequestException)
        {
            this.Log().Error(httpRequestException,
                "Failed to get library infos from server. Firstly, check if you are able to ping to the api. If not, connect administrator.");
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
            var requestUrl = "/libraries/cheatsheet";

#if DEBUG
            requestUrl = "/libraries/cheatsheet?involvePrerelease=true";
#endif

            var responseString = await _client.GetStringAsync(requestUrl);

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
                using var fileStream = File.Open(Path.ChangeExtension(
                    Path.Combine(Constants.LibraryFolder, libraryToUpdate.Name),
                    "vssx"), FileMode.Create, FileAccess.Write);
                await contentStream.CopyToAsync(fileStream);
            }

            await DownloadCheatSheet();

            return libraryToUpdates.Select(ReactiveLibrary.FromLibraryDto);
        }
        catch (HttpRequestException httpRequestException)
        {
            this.Log().Error(httpRequestException,
                "Failed to donwload library from server. Firstly, check if you are able to ping to the api. If not, connect administrator.");
            WindowManager.ShowDialog(
                string.Format(Resources.MSG_server_connect_failed_with_message, httpRequestException.Message),
                MessageBoxButton.OK);
        }
        catch (IOException ioException)
        {
            this.Log().Error(ioException,
                "Failed to overwrite library file. It might be used by other process, consider close it before retry.");
            WindowManager.ShowDialog(string.Format(Resources.MSG_write_file_failed_with_message, ioException.Message),
                MessageBoxButton.OK);
        }

        return [];
    }
}