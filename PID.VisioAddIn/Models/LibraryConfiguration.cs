using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AE.PID.Models;

/// <summary>
///     Defines the library info and update check time and update check interval.
/// </summary>
[Serializable]
public class LibraryConfiguration
{
    /// <summary>
    ///     The next time that a version check will execute.
    /// </summary>
    public DateTime NextTime { get; set; }

    /// <summary>
    ///     The time interval that used to compute the next check time.
    /// </summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromDays(1);

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