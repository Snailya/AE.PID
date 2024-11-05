namespace AE.PID.Visio.Core.Models;

public class VisioShape(CompositeId id, ICollection<VisioShape.ShapeType> types)
{
    public enum ShapeType
    {
        FunctionLocation,
        MaterialLocation,
        None
    }

    public CompositeId Id { get; private set; } = id;
    public ICollection<ShapeType> Types { get; set; } = types;
}