using System.ComponentModel.DataAnnotations;

namespace AE.PID.Server.Data;

/// <summary>
///     Entity framework object for Version.
/// </summary>
public class AppVersion : EntityBase
{
    /// <summary>
    ///     The version string like (major, minor, build, revision)
    /// </summary>
    [MaxLength(50)]
    public string Version { get; set; } = new Version(0, 0, 0, 0).ToString();

    /// <summary>
    ///     The version description.
    /// </summary>
    [MaxLength(8192)]
    public string ReleaseNotes { get; set; } = string.Empty;

    /// <summary>
    ///     The file store path that used for downloading.
    /// </summary>
    [MaxLength(4096)]
    public string PhysicalFile { get; set; } = string.Empty;

    [MaxLength(64)] public string Hash { get; set; } = string.Empty;
}