namespace AE.PID.Client.Core;

public record ProjectLocation(ICompoundKey Id, int? ProjectId) : ILocation
{
    /// <summary>
    ///     The id of the project in PDMS.
    /// </summary>
    public int? ProjectId { get; set; } = ProjectId;

    /// <summary>
    ///     The object id that stores the data.
    /// </summary>
    public ICompoundKey Id { get; } = Id;
}