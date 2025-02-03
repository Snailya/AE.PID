namespace AE.PID.Client.Core.VisioExt.Models;

/// <summary>
///     The identifier used to locate the shape in the visio. Because the shape id is page scoped, so there will be overlap
///     between different pages, so the page id is also recorded to avoid this overlap.
///     Currently, the program if only for one document, so no document id is considered, but it can be adjusted to involve
///     document id in the future.
/// </summary>
/// <param name="pageId"></param>
/// <param name="shapeId"></param>
public class VisioShapeId(int pageId = 0, int shapeId = 0) : CompoundKeyBase
{
    public int PageId { get; } = pageId;
    public int ShapeId { get; } = shapeId;

    public override int ComputedId => (PageId * 397) ^ ShapeId;

    public override string ToString()
    {
        return $"CompositeId {{PageId={PageId}, ShapeId={ShapeId}}}";
    }
}