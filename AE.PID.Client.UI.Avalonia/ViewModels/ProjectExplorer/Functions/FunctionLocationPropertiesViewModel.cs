using AE.PID.Client.Core;
using AE.PID.Client.UI.Avalonia.Shared;
using AE.PID.Core;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public class FunctionLocationPropertiesViewModel : ViewModelBase
{
    private string _description = string.Empty;
    private string _element = string.Empty;
    private int? _functionId;
    private string _group = string.Empty;
    private string _groupEnglishName = string.Empty;
    private string _groupName = string.Empty;
    private bool _isVirtual;
    private string _remarks = string.Empty;
    private int _unitMultiplier;

    private string _zone = string.Empty;
    private string _zoneEnglishName = string.Empty;
    private string _zoneName = string.Empty;

    public FunctionLocationPropertiesViewModel(FunctionLocation source)
    {
        Source = source;

        FunctionId = source.FunctionId;
        FunctionType = source.Type;
        Zone = source.Zone;
        ZoneName = source.ZoneName;
        ZoneEnglishName = source.ZoneEnglishName;
        Group = source.Group;
        GroupName = source.GroupName;
        GroupEnglishName = source.GroupEnglishName;
        Element = source.Element;
        Description = source.Description;
        Remarks = source.Remarks;
        IsVirtual = source.IsVirtual;
        UnitMultiplier = source.UnitMultiplier;
    }

    public bool IsVirtual
    {
        get => _isVirtual;
        set => this.RaiseAndSetIfChanged(ref _isVirtual, value);
    }

    public int? FunctionId
    {
        get => _functionId;
        set => this.RaiseAndSetIfChanged(ref _functionId, value);
    }

    public FunctionLocation Source { get; set; }


    public string Zone
    {
        get => _zone;
        set => this.RaiseAndSetIfChanged(ref _zone, value);
    }

    public string ZoneName
    {
        get => _zoneName;
        set => this.RaiseAndSetIfChanged(ref _zoneName, value);
    }

    public string ZoneEnglishName
    {
        get => _zoneEnglishName;
        set => this.RaiseAndSetIfChanged(ref _zoneEnglishName, value);
    }

    public string Group
    {
        get => _group;
        set => this.RaiseAndSetIfChanged(ref _group, value);
    }

    public string GroupName
    {
        get => _groupName;
        set => this.RaiseAndSetIfChanged(ref _groupName, value);
    }

    public string GroupEnglishName
    {
        get => _groupEnglishName;
        set => this.RaiseAndSetIfChanged(ref _groupEnglishName, value);
    }

    public string Element
    {
        get => _element;
        set => this.RaiseAndSetIfChanged(ref _element, value);
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

    public FunctionType FunctionType { get; set; }

    public int UnitMultiplier
    {
        get => _unitMultiplier;
        set => this.RaiseAndSetIfChanged(ref _unitMultiplier, value);
    }
}