namespace AE.PID.Client.Core.VisioExt.Models;

public class VisioPageId(int id) : CompoundKeyBase
{
    public override int ComputedId { get; } = id;
}