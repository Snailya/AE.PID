namespace AE.PID.Client.Core;

public record MaterialLocation(
    ICompoundKey Id,
    string Code,
    double Quantity,
    double ComputedQuantity,
    string KeyParameters,
    string Category) : ILocation
{
    /// <summary>
    ///     The user-input quantity that bound with this material location. The actual quantity will be quantity x unit
    ///     multiplier.
    /// </summary>
    public double Quantity { get; set; } = Quantity;

    /// <summary>
    ///     The actual quantity of the material, which is the quantity x unit multiplier.
    /// </summary>
    public double ComputedQuantity { get; } = ComputedQuantity;

    /// <summary>
    ///     The technical data that provides hints when processing material selection.
    /// </summary>
    public string KeyParameters { get; } = KeyParameters;

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


    public ICompoundKey Id { get; } = Id;
}