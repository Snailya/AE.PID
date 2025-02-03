namespace AE.PID.Client.Core;

public record ProjectLocation(ICompoundKey Id, int? ProjectId) : ILocation
{
    public int? ProjectId { get; set; } = ProjectId;
    public ICompoundKey Id { get; } = Id;
}