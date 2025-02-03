namespace AE.PID.Client.Core.VisioExt.Models;

public class VisioDocumentId(int id) : CompoundKeyBase
{
    public override int ComputedId { get; } = id;
}