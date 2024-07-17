using System;
using System.Collections.Generic;
using AE.PID.Dtos;

namespace AE.PID.Models;

[Serializable]
public class Configuration
{
    public string Server { get; set; } = "http://172.18.168.35:32768";
    public string UserId { get; set; } = string.Empty;

    public DateTime NextTime { get; set; } = DateTime.Today;

    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromDays(1);

    public LibraryConfiguration LibraryConfiguration { get; set; } = new();
}

[Serializable]
public class LibraryConfiguration
{
    public DateTime NextTime { get; set; } = DateTime.Today;

    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromDays(1);

    public IEnumerable<Library> Libraries { get; set; } = [];
}

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

[Serializable]
public class LibraryItem
{
    /// <summary>
    ///     The name of the item which displayed in visio.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     The Unique id of the item, if the unique id is not equal to the unique id of the item in document stencil,
    ///     indicates that the item used in document stencil is not the same as the library, which means an update is needed.
    /// </summary>
    public string UniqueId { get; set; } = string.Empty;

    /// <summary>
    ///     The id used for deciding which item in a library is of the same origin with the item in document stencil.
    /// </summary>
    public string BaseId { get; set; } = string.Empty;

    public static LibraryItem FromLibraryItemDto(LibraryItemDto dto)
    {
        return new LibraryItem
        {
            Name = dto.Name,
            UniqueId = dto.UniqueId,
            BaseId = dto.BaseId
        };
    }
}