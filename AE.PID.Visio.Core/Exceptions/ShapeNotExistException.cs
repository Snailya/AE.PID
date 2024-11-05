using AE.PID.Visio.Core.Models;

namespace AE.PID.Visio.Core.Exceptions;

/// <summary>
///     When try to select a shape in visio, the shape with specified id may not exist, then this exception raises.
/// </summary>
/// <param name="id"></param>
public class ShapeNotExistException(CompositeId id) : Exception($"未找到满足条件{id}的形状。")
{
}