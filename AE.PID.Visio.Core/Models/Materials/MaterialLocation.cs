using DynamicData.Binding;

namespace AE.PID.Visio.Core.Models;

public class MaterialLocation(CompositeId locationId) : AbstractNotifyPropertyChanged
{
    private string _category = string.Empty;
    private string _code = string.Empty;
    private double _computedQuantity;
    private bool _isExcluded = false;
    private string _keyParameters = string.Empty;
    private CompositeId _locationId = locationId;
    private double _quantity;

    public Guid UniqueId { get; } = new();

    /// <summary>
    ///     The function location that this material location is attached to.
    ///     Currently, one function location can only have one material location, which will be extended in the future.
    /// </summary>
    public CompositeId LocationId
    {
        get => _locationId;
        set => SetAndRaise(ref _locationId, value);
    }

    /// <summary>
    ///     The code of the material that assigned to this location.
    /// </summary>
    public string Code
    {
        get => _code;
        set => SetAndRaise(ref _code, value);
    }

    /// <summary>
    ///     The user-input quantity that bound with this material location. The actual quantity will be quantity x unit
    ///     multiplier.
    /// </summary>
    public double Quantity
    {
        get => _quantity;
        set => SetAndRaise(ref _quantity, value);
    }

    /// <summary>
    ///     The actual quantity of the material, which is the quantity x unit multiplier.
    /// </summary>
    public double ComputedQuantity
    {
        get => _computedQuantity;
        set => SetAndRaise(ref _computedQuantity, value);
    }

    /// <summary>
    ///     The technical data that provides hints when processing material selection.
    /// </summary>
    public string KeyParameters
    {
        get => _keyParameters;
        set => SetAndRaise(ref _keyParameters, value);
    }

    /// <summary>
    ///     The category that the material fails into.
    /// </summary>
    public string Category
    {
        get => _category;
        set => SetAndRaise(ref _category, value);
    }

    /// <summary>
    ///     If this material location is in the internal scope.
    /// </summary>
    public bool IsExcluded
    {
        get => _isExcluded;
        set => SetAndRaise(ref _isExcluded, value);
    }
}