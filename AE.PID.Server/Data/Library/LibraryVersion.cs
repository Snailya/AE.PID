using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AE.PID.Server.Data;
// [Table("library_versions")]

public class LibraryVersion : EntityBase
{
    /// <summary>
    ///     The version string like (major, minor, build, revision)
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    ///     The version description.
    /// </summary>
    public string ReleaseNotes { get; set; } = string.Empty;

    /// <summary>
    ///     Indicates whether this library has been released
    /// </summary>
    public bool IsReleased { get; set; } = false;

    /// <summary>
    ///     The file store path that used for downloading.
    /// </summary>
    [MaxLength(4096)]
    public string FileName { get; set; } = string.Empty;

    #region -- Navigation Properties --

    public int LibraryId { get; set; }
    [ForeignKey("LibraryId")] public Library Library { get; set; }


    public ICollection<RepositorySnapshot> LibrarySnapshots { get; set; }

    public ICollection<LibraryItem> LibraryVersionItems { get; set; }

    #endregion
}