using System.ComponentModel;

namespace AE.PID.Core;

public class AppVersionDto
{
    /// <summary>
    ///     The version string of the application
    /// </summary>
    [Description("版本号")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    ///     The url to download the installer
    /// </summary>
    [Description("下载链接")]
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    ///     The recommended filename
    /// </summary>
    [Description("文件名")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    ///     The hash of the file used for check.
    /// </summary>
    [Description("文件校验值")]
    public string FileHash { get; set; } = string.Empty;

    /// <summary>
    ///     The release note of the installer.
    /// </summary>
    [Description("发布说明")]
    public string ReleaseNotes { get; set; } = string.Empty;

    /// <summary>
    /// The channel this version is pushing through.
    /// </summary>
    [Description("更新通道")] public VersionChannel Channel { get; set; }
}