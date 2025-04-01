namespace AE.PID.Client.Core;

public record Instrument(
    ICompoundKey Id,
    string Code,
    double Quantity,
    int UnitMultiplier,
    string Category,
    string High,
    string Low,
    bool IsVirtual
) : MaterialLocationBase(Id, Code, Quantity, UnitMultiplier, Category, IsVirtual)
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