using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AE.PID.Models.Configurations;

/// <summary>
///     Defines the library info and update check time and update check interval.
/// </summary>
[Serializable]
public class LibraryConfiguration : ConfigurationBase
{
    /// <summary>
    ///     The config for libraries that defines the library name, version, hash and local path.
    /// </summary>
    public ConcurrentBag<Library> Libraries { get; set; } = [];

    /// <summary>
    ///     Get item list of the libraries.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<LibraryItem> GetItems()
    {
        return Libraries.SelectMany(x => x.Items);
    }
}