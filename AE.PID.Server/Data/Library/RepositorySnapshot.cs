namespace AE.PID.Server.Data;

/// <summary>
///     This class stores the snapshot for the libraries. It is used to tracking whether the document needs to update.
/// </summary>
public class RepositorySnapshot : EntityBase
{
    public ICollection<LibraryVersion> Versions { get; set; }
}