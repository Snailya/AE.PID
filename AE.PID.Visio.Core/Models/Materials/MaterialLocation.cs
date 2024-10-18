using DynamicData.Binding;

namespace AE.PID.Visio.Core.Models;

public class MaterialLocation(CompositeId locationId) : AbstractNotifyPropertyChanged
{
    private string _code = string.Empty;
    private string _keyParameters = string.Empty;
    private CompositeId _locationId = locationId;
    private double _quantity;
    private string _type = string.Empty;
    private double _unitQuantity;

    public Guid UniqueId { get; } = new();

    public CompositeId LocationId
    {
        get => _locationId;
        set => SetAndRaise(ref _locationId, value);
    }

    public string Code
    {
        get => _code;
        set => SetAndRaise(ref _code, value);
    }

    public double UnitQuantity
    {
        get => _unitQuantity;
        set => SetAndRaise(ref _unitQuantity, value);
    }

    public double Quantity
    {
        get => _quantity;
        set => SetAndRaise(ref _quantity, value);
    }

    public string KeyParameters
    {
        get => _keyParameters;
        set => SetAndRaise(ref _keyParameters, value);
    }

    public string Type
    {
        get => _type;
        set => SetAndRaise(ref _type, value);
    }
}