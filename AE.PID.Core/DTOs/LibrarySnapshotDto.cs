namespace AE.PID.Core.DTOs;

public class LibrarySnapshotDto
{
    public int Id { get; set; }
    public LibraryVersionDto[] Versions { get; set; } = [];
}

public class LibraryVersionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string DownloadUrl { get; set; }
}