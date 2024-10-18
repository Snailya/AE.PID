using System.ComponentModel.DataAnnotations;

namespace AE.PID.Server.Data;

public class Library : EntityBase
{
    /// <summary>
    ///     The name of the library.
    /// </summary>
    [Required]
    public string Name { get; set; }

    #region -- Navigation Properties --

    public ICollection<LibraryVersion> Versions { get; set; } = [];

    #endregion

    /// <summary>
    ///     Get the latest version of the library.
    /// </summary>
    /// <returns></returns>
    public LibraryVersion? GetLatestVersion(bool involvePrerelease = false)
    {
        return involvePrerelease
            ? Versions.MaxBy(x => new Version(x.Version))
            : Versions.Where(x => x.IsReleased).MaxBy(x => new Version(x.Version));
    }
}