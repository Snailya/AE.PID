namespace PID.Server.Models;

public class UploadLibraryDto
{
    /// <summary>
    ///     The vssx file for installer
    /// </summary>
    public IFormFile File { get; set; }

    /// <summary>
    ///     The name of the library, used for deciding whether its a new library or a new version of the library.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     The version string of current uploaded app.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    ///     The description for the current version, might be something added, or updated.
    /// </summary>
    public string ReleaseNote { get; set; }
}