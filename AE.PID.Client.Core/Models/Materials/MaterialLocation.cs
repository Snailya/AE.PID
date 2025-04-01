namespace AE.PID.Client.Core;

public record MaterialLocation(
    ICompoundKey Id,
    string Code,
    double Quantity,
    int UnitMultiplier,
    string Category,
    string KeyParameters,
    bool IsVirtual,
    ICompoundKey? ProxyGroupId = null,
    ICompoundKey? TargetId = null
) : MaterialLocationBase(Id, Code, Quantity, UnitMultiplier, Category, IsVirtual, ProxyGroupId, TargetId)
{
    /// <summary>
    ///     The technical data that provides hints when processing material selection.
    /// </summary>
    public string KeyParameters { get; } = KeyParameters;
}