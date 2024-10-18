namespace AE.PID.Visio.Core.Models;

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