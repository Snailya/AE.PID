using System;
using System.Collections.Generic;

namespace AE.PID.Models;

/// <summary>
///     Library is the equipment library created and maintained by AE painting visio group. The library is shown as a
///     stencil document is Visio with suffix of vssx.
/// </summary>
[Serializable]
public class Library
{
    /// <summary>
    ///     The identifier used in server request.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     The name of the library, used for user identification.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     The named version, notice that even the version is correct, the file may not be the same as the server as user
    ///     might edit the local file which is not as expected.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    ///     The hash of the server file used for checking if user edit the local file. The app should prompt user to notice
    ///     that the local version is not the same as the server.
    /// </summary>
    public string Hash { get; set; }

    /// <summary>
    ///     The path of the local file which used to load the local file to active document when user click library button in
    ///     the ribbon, and to persist file download from server.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    ///     Then content of the library.
    /// </summary>
    public IEnumerable<LibraryItem> Items { get; set; } = new List<LibraryItem>();

    public override string ToString()
    {
        return $"{Name}: {Version}";
    }
}