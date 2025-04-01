using System.Xml.Serialization;

namespace AE.PID.Client.Core.VisioExt;

/// <summary>
///     The identifier used to locate the shape in the visio. Because the shape id is page scoped, so there will be overlap
///     between different pages, so the page id is also recorded to avoid this overlap.
///     Currently, the program if only for one document, so no document id is considered, but it can be adjusted to involve
///     document id in the future.
/// </summary>
public class VisioShapeId : CompoundKeyBase
{
    public VisioShapeId(int pageId = 0, int shapeId = 0)
    {
        PageId = pageId;
        ShapeId = shapeId;
    }

    private VisioShapeId()
    {
        // for serializing only
    }

    [XmlElement("PageId")] public int PageId { get; set; }

    [XmlElement("ShapeId")] public int ShapeId { get; set; }

    public override int ComputedId => (PageId * 397) ^ ShapeId;

    public static VisioShapeId Default => new();

    public override string ToString()
    {
        return $"{{PageId: {PageId}, ShapeId: {ShapeId}}}";
    }

    public bool IsVirtualShape()
    {
        return PageId < 0;
    }
}