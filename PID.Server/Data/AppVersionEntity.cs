namespace AE.PID.Server.Data;

/// <summary>
///     Entity framework object for Version.
/// </summary>
public class AppVersionEntity
{
    /// <summary>
    ///     The identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     The version string like (major, minor, build, revision)
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    ///     The version description.
    /// </summary>
    public string ReleaseNotes { get; set; }

    /// <summary>
    ///     The file store path that used for downloading.
    /// </summary>
    public string FileName { get; set; }
}