using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using AE.PID.Core;

namespace AE.PID.Server.Data;

/// <summary>
///     Entity framework object for Version.
/// </summary>
public partial class AppVersion : EntityBase
{
    // 新增数据库可排序字段
    public int Major { get; set; }
    public int Minor { get; set; }
    public int Build { get; set; }
    public int Revision { get; set; }

    /// <summary>
    ///     The version string like (major, minor, build, revision)
    /// </summary>
    [MaxLength(50)]
    public string Version
    {
        get => $"{Major}.{Minor}.{Build}.{Revision}";
        set
        {
            if (!IsValidVersion(value))
                throw new ArgumentException($"Invalid version format: {value}");
            ParseVersion(value);
        }
    }

    /// <summary>
    ///     The version description.
    /// </summary>
    [MaxLength(8192)]
    public string ReleaseNotes { get; set; } = string.Empty;

    public VersionChannel Channel { get; set; } = VersionChannel.InternalTesting;

    /// <summary>
    ///     The file store path that used for downloading.
    /// </summary>
    [MaxLength(4096)]
    public string PhysicalFile { get; set; } = string.Empty;

    [MaxLength(64)] public string Hash { get; set; } = string.Empty;

    private void ParseVersion(string versionString)
    {
        var parts = versionString.Split('.', 4);
        Major = int.Parse(parts[0]);
        Minor = parts.Length > 1 ? int.Parse(parts[1]) : 0;
        Build = parts.Length > 2 ? int.Parse(parts[2]) : 0;
        Revision = parts.Length > 3 ? int.Parse(parts[3]) : 0;
    }

    private static bool IsValidVersion(string version)
    {
        return MyRegex().IsMatch(version);
    }

    [GeneratedRegex(@"^(\d+)(\.\d+){0,3}$")]
    private static partial Regex MyRegex();
}