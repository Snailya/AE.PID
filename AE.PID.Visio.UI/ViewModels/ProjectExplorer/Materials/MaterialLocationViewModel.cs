using System;
using System.Reactive.Disposables;
using AE.PID.Visio.Core.Models;
using AE.PID.Visio.Core.Models.Projects;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class MaterialLocationViewModel : ReactiveObject
{
    private readonly CompositeDisposable _cleanUp = new();
    private readonly FunctionLocation? _function;
    private string _description = string.Empty;
    private bool _isEnabled = true;
    private bool _isSelected;
    private string _materialCode;
    private string _remarks = string.Empty;
    private double _unitQuantity;
    private string _fullText;

    public CompositeId FunctionId { get; }
    public string MaterialType { get; set; }

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
    
    public string ProcessArea => _function?.Zone ?? string.Empty;
    public string FunctionalGroup => _function?.Group ?? string.Empty;
    public string FunctionalElement => _function?.Element ?? string.Empty;
    public string KeyParameters { get; }
    public double Quantity { get; set; }

    public double UnitQuantity
    {
        get => _unitQuantity;
        set => this.RaiseAndSetIfChanged(ref _unitQuantity, value);
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

    public void SetMaterial(MaterialViewModel material)
    {
        MaterialCode = material.Code;
    }

    public void Dispose()
    {
        _cleanUp.Dispose();
    }
    
    #region -- Constuctor --

    public MaterialLocationViewModel(MaterialLocation material, FunctionLocation function) :
        this(material)
    {
        _function = function;
        _description = function.Description;
        _remarks = function.Remarks;

        this.WhenValueChanged(x => x.Description, false)
            .WhereNotNull()
            .Subscribe(x => { _function.Description = x; })
            .DisposeWith(_cleanUp);
        this.WhenValueChanged(x => x.Remarks, false)
            .WhereNotNull()
            .Subscribe(x => { _function.Remarks = x; })
            .DisposeWith(_cleanUp);
    }
    
    public bool Contains(string text){
        var lowercaseText = text.ToLower();
        return ProcessArea.ToLower().Contains(lowercaseText) || FunctionalGroup.ToLower().Contains(lowercaseText) || FunctionalElement.ToLower().Contains(lowercaseText) || MaterialType.ToLower().Contains(lowercaseText) || KeyParameters.ToLower().Contains(lowercaseText) || Description.ToLower().Contains(lowercaseText) || Remarks.ToLower().Contains(lowercaseText);
        }

    public MaterialLocationViewModel(MaterialLocation material)
    {
        FunctionId = material.LocationId;

        _unitQuantity = material.UnitQuantity;
        _materialCode = material.Code;

        Quantity = material.Quantity;
        KeyParameters = material.KeyParameters;
        MaterialType = material.Type;

        this.WhenAnyValue(x => x.MaterialCode)
            .Subscribe(x => { material.Code = x; })
            .DisposeWith(_cleanUp);
        this.WhenAnyValue(x => x.Quantity)
            .Subscribe(x => { material.Quantity = x; })
            .DisposeWith(_cleanUp);
    }

    #endregion
}