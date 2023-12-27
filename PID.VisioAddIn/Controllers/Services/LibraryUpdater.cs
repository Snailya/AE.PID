using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using AE.PID.Models;
using NLog;
using PID.Core.Dtos;

namespace AE.PID.Controllers.Services;

/// <summary>
/// Compare local library configuration with the server, and download newest if exist.
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
    ///  Automatically check the server for library updates and done in silent.
    /// The check interval is control by configuration.
    /// </summary>
    /// <returns></returns>
    public static IDisposable Listen()
    {
        Logger.Info($"Library Update Service started.");

        return Globals.ThisAddIn.Configuration.LibraryConfiguration.CheckIntervalSubject // auto check observable
            .Select(Observable.Interval)
            .Switch()
            .Merge(Observable.Return<long>(-1))
            .Where(_ =>
                Globals.ThisAddIn.Configuration.LibraryConfiguration.NextTime == null ||
                DateTime.Now > Globals.ThisAddIn.Configuration.LibraryConfiguration.NextTime)
            .Do(_ => Logger.Info($"Library Update started. {{Initiated by: Auto-Run}}"))
            // merge with user manually invoke observable
            .Merge(
                ManuallyInvokeTrigger.Throttle(TimeSpan.FromMilliseconds(300))
                    .Select(_ => Constants.ManuallyInvokeMagicNumber)
                    .Do(_ => Logger.Info($"Library Update started. {{Initiated by: User}}"))
            )
            // perform check
            .SelectMany(
                _ => Observable
                    .FromAsync(UpdateLibrariesAsync),
                (value, result) => new { InvokeType = value, Result = result }
            )
            // notify user if need
            .Select(data =>
            {
                // prompt an alert to let user know update completed if it's invoked by user.
                if (data.InvokeType == Constants.ManuallyInvokeMagicNumber)
                    ThisAddIn.Alert("更新完毕");

                return data.Result;
            })
            // as http request may have error, retry for next emit
            .Retry(3)
            // for error handling only
            .Subscribe(
                _ => { Logger.Info($"Libraries are up to date."); },
                ex =>
                {
                    ThisAddIn.Alert(ex.Message);
                    Logger.Error(ex,
                        $"Library Update Service ternimated accidently.");
                },
                () => { Logger.Error("Library Update Service should never completed."); });
    }

    /// <summary>
    /// Update local library file and configuration.
    /// </summary>
    private static async Task<List<Library>> UpdateLibrariesAsync()
    {
        var client = Globals.ThisAddIn.HttpClient;
        var configuration = Globals.ThisAddIn.Configuration;

        try
        {
            var servers = (await client.GetFromJsonAsync<IEnumerable<LibraryDto>>(configuration.Api + "/libraries"))
                .ToList();

            var updatedLibraries = new List<Library>();

            Logger.Info(
                $"Libraries version:  {{{string.Join(",", servers.Select(x => $"{x.Name}: {x.Version}"))} }}");

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
            configuration.LibraryConfiguration.Libraries = new ConcurrentBag<Library>(updatedLibraries);
            Configuration.Save(configuration);

            return updatedLibraries;
        }
        catch (HttpRequestException httpRequestException)
        {
            Logger.Error(httpRequestException,
                "Failed to donwload library from server. Firstly, check if you are able to ping to the api. If not, connect administrator.");
            throw;
        }
    }
}