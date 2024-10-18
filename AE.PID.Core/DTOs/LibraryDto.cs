using System.Collections.Generic;

namespace AE.PID.Core.DTOs;

/// <summary>
///     The response dto used for get /libraries
/// </summary>
public class LibraryDto
{
    /// <summary>
    ///     The id of the library that can used for download the latest file.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     The name of the library that will be used as the filename in local storage and configuration.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     The version string of the library's latest version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    ///     The download url that used to get the latest version of the library file.
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    ///     The items inside the library.
    /// </summary>
    public IEnumerable<LibraryItemDto> Items { get; set; } = [];
}