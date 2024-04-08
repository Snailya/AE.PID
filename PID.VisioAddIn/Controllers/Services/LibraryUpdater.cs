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
using AE.PID.Core.DTOs;
using AE.PID.Models.Configurations;
using Microsoft.Office.Interop.Visio;
using Newtonsoft.Json;
using NLog;
using Path = System.IO.Path;

namespace AE.PID.Controllers.Services;

/// <summary>
///     Compare local library configuration with the server, and download newest if exist.
/// </summary>
public abstract class LibraryUpdater
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static Subject<Unit> ManuallyInvokeTrigger { get; } = new();

    /// <summary>
    ///     Emit a value manually
    /// </summary>
    public static void Invoke()
    {
        ManuallyInvokeTrigger.OnNext(Unit.Default);
    }

    /// <summary>
    ///     Automatically check the server for library updates and done in silent.
    ///     The check interval is control by configuration.
    /// </summary>
    /// <returns></returns>
    public static IDisposable Listen()
    {
        Logger.Info("Library Update Service started.");

        return Globals.ThisAddIn.Configuration.LibraryConfiguration.CheckIntervalSubject // auto check observable
            .Select(Observable.Interval)
            .Switch()
            .Merge(Observable.Return<long>(-1))
            .Where(_ =>
                DateTime.Now > Globals.ThisAddIn.Configuration.LibraryConfiguration.NextTime)
            .Do(_ => Logger.Info("Library Update started. {Initiated by: Auto-Run}"))
            // merge with user manually invoke observable
            .Merge(
                ManuallyInvokeTrigger.Throttle(TimeSpan.FromMilliseconds(300))
                    .Select(_ => Constants.ManuallyInvokeMagicNumber)
                    .Do(_ => Logger.Info("Library Update started. {Initiated by: User}"))
            ).Subscribe(
                DoUpdate,
                ex => { Logger.Error(ex, "Library Update Service ternimated accidently."); },
                () => { Logger.Error("Library Update Service should never completed."); });
    }

    public static async Task<List<LibraryDto>?> GetLibraries()
    {
        var client = Globals.ThisAddIn.HttpClient;

        var requestUrl = "/libraries";
#if DEBUG
        requestUrl = "/libraries?involvePrerelease=true";
#endif
        var response = await client.GetStringAsync(requestUrl);

        return JsonConvert.DeserializeObject<IEnumerable<LibraryDto>>(response)?.ToList();
    }

    private static void DoUpdate(long seed)
    {
        _ = Observable.Return(seed)
            // When updating, the new file stream will be write to the origin file if the file exist.
            // However, this origin file might already opened in Visio which leads to busy situation.
            // So, before do updates, first close all that stencils and restore after update.
            // Notice that this close process should be called on main thread as if alters the UI
            .ObserveOn(Globals.ThisAddIn.SynchronizationContext)
            .Select(data =>
            {
                var dockedStencils = Globals.ThisAddIn.Application.Documents
                    .OfType<IVDocument>()
                    .Where(x => x.Type == VisDocumentTypes.visTypeStencil)
                    .ToList();
                var paths = dockedStencils.Select(x => x.FullName).ToList();

                foreach (var item in dockedStencils)
                    item.Close();

                return new { InvokeType = data, StencilsToRestore = paths };
            })
            // After closed, perform the update process in a background thread.
            .ObserveOn(ThreadPoolScheduler.Instance)
            .SelectMany(
                _ => Observable
                    .FromAsync(UpdateLibrariesAsync),
                (data, result) => new
                {
                    data.InvokeType,
                    data.StencilsToRestore,
                    Result = result
                }
            )
            .Do(_ => { Logger.Info("Libraries are up to date."); })
            .Do(_ => DownloadCheatSheet())
            // notify user if need
            .Where(x => x.InvokeType == Constants.ManuallyInvokeMagicNumber)
            .ObserveOn(Globals.ThisAddIn.SynchronizationContext)
            .Select(data =>
            {
                foreach (var item in data.StencilsToRestore)
                    Globals.ThisAddIn.Application.Documents.OpenEx(item,
                        (short)VisOpenSaveArgs.visOpenDocked);

                ThisAddIn.Alert("更新完毕");
                return Unit.Default;
            })
            .Subscribe(
                _ => { },
                ex =>
                {
                    switch (ex)
                    {
                        case HttpRequestException httpRequestException:
                            Logger.Error(httpRequestException,
                                "Failed to donwload library from server. Firstly, check if you are able to ping to the api. If not, connect administrator.");
                            ThisAddIn.Alert("无法连接至服务器，请检查网络。");
                            break;
                        case IOException ioException:
                            Logger.Error(ioException,
                                "Failed to overwrite library file. It might be used by other process, consider close it before retry.");
                            ThisAddIn.Alert($"无法写入数据，文件被占用。{ioException.Message}");
                            break;
                    }
                });
    }

    private static async Task DownloadCheatSheet()
    {
        var requestUrl = "/libraries/cheatsheet";

#if DEBUG
        requestUrl = "/libraries/cheatsheet?involvePrerelease=true";
#endif

        var responseString = await Globals.ThisAddIn.ServiceManager.Client.GetStringAsync(        requestUrl);

        if (string.IsNullOrEmpty(responseString)) return;
        var responseResult = JsonConvert.DeserializeObject<IEnumerable<DetailedLibraryItemDto>>(responseString);
        Debug.Assert(responseResult != null);

        using var writer = new StreamWriter(ThisAddIn.LibraryCheatSheet);
        await writer.WriteLineAsync(responseString);
    }


    private static async Task<List<Library>> UpdateLibrariesAsync()
    {
        var client = Globals.ThisAddIn.HttpClient;
        var configuration = Globals.ThisAddIn.Configuration;

        var servers = await GetLibraries();
        Logger.Info(
            $"Libraries version:  {{{string.Join(",", servers.Select(x => $"{x.Name}: {x.Version}"))} }}");

        var updatedLibraries = new List<Library>();
        foreach (var server in servers)
        {
            var local = configuration.LibraryConfiguration.Libraries.SingleOrDefault(x => x.Id == server.Id);
            if (local != null && local.Version == server.Version)
            {
                updatedLibraries.Add(local);
                continue;
            }

            local ??= new Library
            {
                Id = server.Id,
                Items = server.Items.Select(x => new LibraryItem
                    { BaseId = x.BaseId, Name = x.Name, UniqueId = x.UniqueId }),
                Name = server.Name,
                Version = server.Version,
                Path = Path.GetFullPath(Path.ChangeExtension(Path.Combine(ThisAddIn.LibraryFolder, server.Name),
                    "vssx"))
            };

            using var response = await client.GetAsync(server.DownloadUrl);
            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = File.Open(local.Path, FileMode.Create, FileAccess.Write);
            await contentStream.CopyToAsync(fileStream);

            if (configuration.LibraryConfiguration.Libraries.All(x => x.Id != local.Id))
            {
                configuration.LibraryConfiguration.Libraries.Add(local);
            }
            else
            {
                local.Name = server.Name;
                local.Version = server.Version;
                local.Items = server.Items.Select(x => new LibraryItem
                    { BaseId = x.BaseId, Name = x.Name, UniqueId = x.UniqueId });
            }

            updatedLibraries.Add(local);
        }

        configuration.LibraryConfiguration.NextTime =
            DateTime.Now + configuration.LibraryConfiguration.CheckInterval;
        configuration.LibraryConfiguration.Libraries = [..updatedLibraries];
        configuration.Save();

        return updatedLibraries;
    }
}