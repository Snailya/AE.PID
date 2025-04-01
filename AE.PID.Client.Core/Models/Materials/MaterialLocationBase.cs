namespace AE.PID.Client.Core;

public abstract record MaterialLocationBase(
    ICompoundKey Id,
    string Code,
    double Quantity,
    int UnitMultiplier,
    string Category,
    bool IsVirtual,
    ICompoundKey? ProxyGroupId = null,
    ICompoundKey? TargetId = null) : ILocation
{
    /// <summary>
    ///     The user-input quantity that bound with this material location. The actual quantity will be quantity x unit
    ///     multiplier.
    /// </summary>
    public double Quantity { get; set; } = Quantity;

    /// <summary>
    ///     The actual quantity of the material, which is the quantity x unit multiplier.
    /// </summary>
    public double ComputedQuantity => Quantity * UnitMultiplier;

    /// <summary>
    ///     The category that the material fails into.
    /// </summary>
    public string Category { get; } = Category;

    /// <summary>
    ///     If this material location is in the internal scope.
    /// </summary>
    public bool IsExcluded { get; set; }

    /// <summary>
    ///     The code of the material that assigned to this location.
    /// </summary>
    public string Code { get; set; } = Code;

    public bool IsVirtual { get; set; } = IsVirtual;

    public ICompoundKey? ProxyGroupId { get; } = ProxyGroupId;
    public ICompoundKey? TargetId { get; } = TargetId;

    public int UnitMultiplier { get; set; } = UnitMultiplier;

    public ICompoundKey Id { get; } = Id;
}