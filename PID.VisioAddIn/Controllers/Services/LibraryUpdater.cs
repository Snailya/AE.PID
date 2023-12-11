using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AE.PID.Models;
using PID.Core.Dtos;

namespace AE.PID.Controllers.Services;

public abstract class LibraryUpdater
{
    public static async Task<IEnumerable<Library>> UpdateLibrariesAsync()
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

        return updatedLibraries;
    }
}