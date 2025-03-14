namespace AE.PID.Core;

public class AppVersionDto
{
    /// <summary>
    ///     The version string of the application
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    ///     The url to download the installer
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    ///     The recommended filename
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    ///     The hash of the file used for check.
    /// </summary>
    public string FileHash { get; set; } = string.Empty;

    /// <summary>
    ///     The release note of the installer.
    /// </summary>
    public string ReleaseNotes { get; set; } = string.Empty;
}