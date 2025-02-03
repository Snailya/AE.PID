namespace AE.PID.Client.Core.VisioExt.Models;

public class VisioShape(VisioShapeId id, ICollection<LocationType> types)
{
    /// <summary>
    ///     The compound id of the shape
    /// </summary>
    public VisioShapeId Id { get; private set; } = id;

    /// <summary>
    ///     The location type that this shape belongs to.
    /// </summary>
    public ICollection<LocationType> Types { get; set; } = types;

    public ICollection<string> ChangedProperties { get; set; } = [];
}