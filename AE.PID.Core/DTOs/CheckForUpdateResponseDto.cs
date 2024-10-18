namespace AE.PID.Core.DTOs;

public class CheckForUpdateResponseDto
{
    public bool HasUpdate { get; set; }
    public string LatestVersion { get; set; }
    public string DownloadUrl { get; set; }
    public string ReleaseNotes { get; set; }
}