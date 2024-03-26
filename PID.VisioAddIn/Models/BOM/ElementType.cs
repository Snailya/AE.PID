namespace AE.PID.Models.BOM;

public enum ElementType
{
    /// <summary>
    ///     A unit element is converted from a Unit in Visio
    /// </summary>
    Unit,

    /// <summary>
    ///     A single element is converted from a shape with no attached shapes to it
    /// </summary>
    Single,

    /// <summary>
    ///     An attached element is converted from a functional element
    /// </summary>
    Attached
}