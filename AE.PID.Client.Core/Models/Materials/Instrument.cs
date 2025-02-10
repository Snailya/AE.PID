namespace AE.PID.Client.Core;

public record Instrument(
    ICompoundKey Id,
    string Code,
    double Quantity,
    double ComputedQuantity,
    string Category,
    string High,
    string Low
) : MaterialLocationBase(Id, Code, Quantity, ComputedQuantity, Category)
{
    /// <summary>
    ///     The technical data that provides hints when processing material selection.
    /// </summary>
    public string High { get; } = High;

    /// <summary>
    ///     The technical data that provides hints when processing material selection.
    /// </summary>
    public string Low { get; } = Low;
}