namespace AE.PID.Client.Core.VisioExt.Exceptions;

/// <summary>
///     When try to select a shape in visio, the shape with specified id may not exist, then this exception raises.
/// </summary>
/// <param name="id"></param>
public class ShapeNotException(VisioShapeId id) : ItemNotFoundException(id.ToString())
{
}