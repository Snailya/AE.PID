namespace AE.PID.Server.DTOs;

public class UploadLibraryDto
{
    /// <summary>
    ///     The vssx file for installer
    /// </summary>
    public IFormFile File { get; set; }

    /// <summary>
    ///     The description for the current version, might be something added, or updated.
    /// </summary>
    public string ReleaseNote { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this is a minor update
    /// </summary>
    public bool IsMinorUpdate { get; set; } = false;
}