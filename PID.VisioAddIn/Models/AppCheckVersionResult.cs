namespace AE.PID.Models;

public class AppCheckVersionResult
{
    /// <summary>
    ///     Indicates whether a update is available at DownloadUrl.
    /// </summary>
    public bool IsUpdateAvailable { get; set; }

    /// <summary>
    ///     The request url to get the latest version app.
    /// </summary>
    public string DownloadUrl { get; set; }

    /// <summary>
    ///     The release information about the latest version.
    /// </summary>
    public string ReleaseNotes { get; set; }
}