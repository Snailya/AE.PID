namespace AE.PID.Client.Core.VisioExt.Models;

public class VisioMasterId(string baseId, string uniqueId) : CompoundKeyBase
{
    public string BaseId { get; } = baseId;
    public string UniqueId { get; } = uniqueId;
    public override int ComputedId => (BaseId + UniqueId).GetHashCode();
}