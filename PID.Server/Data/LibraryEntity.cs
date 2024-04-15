namespace AE.PID.Server.Data;

public class LibraryEntity
{
    /// <summary>
    ///     The id of the library
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     The name of the library.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     The published version of the library.
    /// </summary>
    public ICollection<LibraryVersionEntity> Versions { get; set; } = new List<LibraryVersionEntity>();

    /// <summary>
    ///     Get the latest version of the library.
    /// </summary>
    /// <returns></returns>
    public LibraryVersionEntity? GetLatestVersion(bool involvePrerelease = false)
    {
        return involvePrerelease
            ? Versions.MaxBy(x => new Version(x.Version))
            : Versions.Where(x => x.IsReleased).MaxBy(x => new Version(x.Version));
    }
}