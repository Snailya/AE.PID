using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using AE.PID.Models;
using PID.Core.Dtos;

namespace AE.PID.Controllers.Services;

/// <summary>
/// Compare local library configuration with the server, and download newest if exist.
/// </summary>
public abstract class LibraryUpdater
{
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
    /// Update local library file and configuration.
    /// </summary>
    public static async Task<List<Library>> UpdateLibrariesAsync()
    {
        var client = Globals.ThisAddIn.HttpClient;
        var configuration = Globals.ThisAddIn.Configuration;

        var servers = await client.GetFromJsonAsync<IEnumerable<LibraryDto>>(configuration.Api + "/libraries");

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
                Path = Path.GetFullPath(Path.ChangeExtension(
                    Path.Combine(Globals.ThisAddIn.DataFolder, "Libraries", server.Name),
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

        configuration.LibraryConfiguration.NextTime = DateTime.Now + configuration.LibraryConfiguration.CheckInterval;
        configuration.LibraryConfiguration.Libraries = new ConcurrentBag<Library>(updatedLibraries);
        Configuration.Save(configuration);

        return updatedLibraries;
    }
}