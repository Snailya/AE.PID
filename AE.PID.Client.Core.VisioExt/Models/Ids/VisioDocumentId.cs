namespace AE.PID.Client.Core.VisioExt;

public class VisioDocumentId(int id) : CompoundKeyBase
{
    public override int ComputedId { get; } = id;
}