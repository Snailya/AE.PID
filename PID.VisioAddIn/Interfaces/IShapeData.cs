namespace AE.PID.Interfaces;

public interface IShapeData : IValueProp
{
    /// <summary>
    ///     Specifies the label that appears to users in the Shape Data window. A label consists of alphanumeric characters,
    ///     including the underscore (_) character.
    /// </summary>
    public string Label { get; }

    /// <summary>
    ///     Specifies the formatting of a shape data item that is a string, a fixed list, a number, a variable list, a date or
    ///     time, a duration, or a currency.
    /// </summary>
    public string Format { get; }

    /// <summary>
    ///     Specifies a data type for the shape data value.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    ///     Evaluates to a string that influences the order in which items in the Shape Data window are listed.
    /// </summary>
    public string SortKey { get; set; }

    /// <summary>
    ///     Specifies whether the shape data item is visible in the Shape Data window.
    /// </summary>
    public string Invisible { get; set; }
}