using System;
using System.Collections.Generic;

namespace AE.PID.Models.Configurations;

/// <summary>
///     Defines the library info and update check time and update check interval.
/// </summary>
[Serializable]
public class LibraryConfiguration
{
    public DateTime NextTime { get; set; } = DateTime.Today;

    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromDays(1);

    public IEnumerable<Library> Libraries { get; set; } = [];
}