using System.Xml.Serialization;

namespace AE.PID.Client.Core.VisioExt;

public class LocationOverlay
{
    private LocationOverlay()
    {
        // for serializing only
    }

    public LocationOverlay(VirtualLocationKey key)
    {
        Key = key;
    }

    [XmlElement("VirtualLocationKey")] public VirtualLocationKey Key { get; set; } = null!;
    [XmlElement("Quantity")] public double? Quantity { get; set; }

    [XmlElement("UnitMultiplier")] public int? UnitMultiplier { get; set; }
    [XmlElement("Code")] public string? Code { get; set; }
    [XmlElement("Description")] public string? Description { get; set; }
    [XmlElement("Remarks")] public string? Remarks { get; set; }

    public bool IsEmpty => Quantity == null && Code == null && Description == null && UnitMultiplier == null &&
                           Remarks == null;

    public override string ToString()
    {
        return
            $"{{Key: {Key}, Description: {Description}, Remarks: {Remarks} UnitMultiplier: {UnitMultiplier}, Quantity: {Quantity}, Code: {Code}}}";
    }
}

public record VirtualLocationKey
{
    private VirtualLocationKey()
    {
        // for serializing only
    }

    public VirtualLocationKey(VisioShapeId proxyGroupId, VisioShapeId targetId)
    {
        ProxyGroupId = proxyGroupId;
        TargetId = targetId;
    }

    [XmlElement("ProxyGroupId")] public VisioShapeId ProxyGroupId { get; set; } = null!;

    [XmlElement("TargetId")] public VisioShapeId TargetId { get; set; } = null!;

    public override string ToString()
    {
        return $"{{ProxyGroupId: {ProxyGroupId}, TargetId: {TargetId}}}";
    }
}