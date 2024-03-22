namespace AE.PID.Server.Data;

public class LibraryVersionEntity
{
    public int LibraryId { get; set; }
    public LibraryEntity Library { get; set; }

    /// <summary>
    ///     The identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     The version string like (major, minor, build, revision)
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    ///     The version description.
    /// </summary>
    public string ReleaseNotes { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this library has been released
    /// </summary>
    public bool IsReleased { get; set; } = false;

    /// <summary>
    ///     The file store path that used for downloading.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    ///     The hash value used to check equity.
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    ///     The item info related to this library version.
    /// </summary>
    public ICollection<LibraryItemEntity> Items { get; set; } = new List<LibraryItemEntity>();
}