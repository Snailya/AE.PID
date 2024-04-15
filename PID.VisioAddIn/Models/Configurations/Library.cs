using System;
using System.Collections.Generic;

namespace AE.PID.Models.Configurations;

/// <summary>
///     Library is the equipment library created and maintained by AE painting visio group. The library is shown as a
///     stencil document is Visio with suffix of vssx.
/// </summary>
[Serializable]
public class Library
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Hash { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public IEnumerable<LibraryItem> Items { get; set; } = new List<LibraryItem>();
}