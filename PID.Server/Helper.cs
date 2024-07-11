using AE.PID.Core.DTOs;
using AE.PID.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace AE.PID.Server;

public class Helper
{
    /// <summary>
    ///     Get the latest items from the library.
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="involvePreRelease"></param>
    /// <returns></returns>
    internal static IEnumerable<DetailedLibraryItemDto> PopulatesCheatSheetItems(AppDbContext dbContext, bool involvePreRelease = false)
    {
        var items = new List<DetailedLibraryItemDto>();

        foreach (var library in dbContext.Libraries)
        {
            var version = dbContext.Entry(library).Collection(v => v.Versions).Query()
                .Where(v => involvePreRelease || v.IsReleased)
                .AsEnumerable()
                .MaxBy(x => new Version(x.Version));
            if (version == null) continue;
            items.AddRange(dbContext.Entry(version).Collection(x => x.Items)
                .Query().Select(x => x.ToDetailedLibraryItemDto()));
        }

        return items;
    }
}