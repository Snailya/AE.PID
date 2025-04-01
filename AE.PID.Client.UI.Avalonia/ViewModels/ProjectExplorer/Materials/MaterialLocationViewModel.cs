using System;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public class MaterialLocationViewModel(MaterialLocation location, Lazy<Task<ResolveResult<Material?>>> material)
    : ReactiveObject, IEquatable<MaterialLocationViewModel>
{
    private string _description = string.Empty;
    private bool _isEnabled = true;
    private bool _isSelected;
    private string _materialCode = location.Code;
    private double _quantity = location.Quantity;
    private string _remarks = string.Empty;

    public ICompoundKey Id { get; } = location.Id;
    public MaterialLocation MaterialSource { get; } = location;
    public FunctionLocation? FunctionSource { get; }
    public string MaterialType { get; set; } = location.Category;

    public string MaterialCode
    {
        get => _materialCode;
        set => this.RaiseAndSetIfChanged(ref _materialCode, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    public string ProcessArea => FunctionSource?.Zone ?? string.Empty;
    public string FunctionalGroup => FunctionSource?.Group ?? string.Empty;
    public string FunctionalElement => FunctionSource?.Element ?? string.Empty;
    public string KeyParameters { get; } = location.KeyParameters;
    public double ComputedQuantity { get; set; } = location.ComputedQuantity;

    public double Quantity
    {
        get => _quantity;
        set => this.RaiseAndSetIfChanged(ref _quantity, value);
    }

    public string Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }

    public string Remarks
    {
        get => _remarks;
        set => this.RaiseAndSetIfChanged(ref _remarks, value);
    }

    public bool IsVirtual { get; } = location.IsVirtual;

    public bool Equals(MaterialLocationViewModel? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return _description == other._description
               && _materialCode == other._materialCode
               && _quantity.Equals(other._quantity)
               && _remarks == other._remarks
               && Id.Equals(other.Id)
               && MaterialType == other.MaterialType
               && KeyParameters == other.KeyParameters;
    }

    #region -- Constuctor --

    public MaterialLocationViewModel(MaterialLocation material, FunctionLocation function,
        Lazy<Task<ResolveResult<Material?>>> materialLoader) :
        this(material, materialLoader)
    {
        FunctionSource = function;
        _description = function.Description;
        _remarks = function.Remarks;
    }

    public bool Contains(string text)
    {
        var lowercaseText = text.ToLower();
        return ProcessArea.ToLower().Contains(lowercaseText) || FunctionalGroup.ToLower().Contains(lowercaseText) ||
               FunctionalElement.ToLower().Contains(lowercaseText) || MaterialType.ToLower().Contains(lowercaseText) ||
               KeyParameters.ToLower().Contains(lowercaseText) || Description.ToLower().Contains(lowercaseText) ||
               Remarks.ToLower().Contains(lowercaseText);
    }

    public async Task<ResolveResult<Material?>> GetMaterial()
    {
        return await material.Value;
    }

    #endregion
}