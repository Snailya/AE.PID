using System;
using System.Collections.Generic;
using System.Linq;

namespace AE.PID.Server.Data;

public class LibraryEntity
{
    /// <summary>
    ///     The id of the library
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The name of the library.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     The published version of the library.
    /// </summary>
    public ICollection<LibraryVersionEntity> Versions { get; set; } = new List<LibraryVersionEntity>();

    /// <summary>
    ///     Get the latest version of the library.
    /// </summary>
    /// <returns></returns>
    public LibraryVersionEntity? GetLatestVersion()
    {
        return Versions.MaxBy(x => new Version(x.Version));
    }
}