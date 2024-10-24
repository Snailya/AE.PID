namespace AE.PID.Visio.Core.Models;

/// <summary>
///     The identifier used to locate the shape in the visio. Because the shape id is page scoped, so there will be overlap
///     between different pages, so the page id is also recorded to avoid this overlap.
///     Currently, the program if only for one document, so no document id is considered, but it can be adjusted to involve
///     document id in the future.
/// </summary>
/// <param name="pageId"></param>
/// <param name="shapeId"></param>
public class CompositeId(int pageId = 0, int shapeId = 0) : IEquatable<CompositeId>
{
    public int PageId { get; } = pageId;
    public int ShapeId { get; } = shapeId;

    public bool Equals(CompositeId? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return PageId == other.PageId && ShapeId == other.ShapeId;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((CompositeId)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (PageId * 397) ^ ShapeId;
        }
    }

    public override string ToString()
    {
        return $"CompositeId {{PageId={PageId}, ShapeId={ShapeId}}}";
    }
}