using System;
using System.Reactive.Linq;
using AE.PID.Core.Models;
using AE.PID.Visio.Core.Models;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class FunctionLocationPropertiesViewModel : ViewModelBase
{
    private string _description;
    private string _element;
    private string _group;
    private string _groupEnglishName;
    private string _groupName;
    private string _remarks;

    private string _zone;
    private string _zoneEnglishName;
    private string _zoneName;

    public FunctionLocationPropertiesViewModel(FunctionLocation source)
    {
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

        #region Subscriptions

        source.WhenAnyPropertyChanged()
            .Throttle(TimeSpan.FromMilliseconds(400))
            .WhereNotNull()
            .Subscribe(value =>
            {
                Zone = value.Zone;
                ZoneName = value.ZoneName;
                ZoneEnglishName = value.ZoneEnglishName;
                Group = value.Group;
                GroupName = value.GroupName;
                GroupEnglishName = value.GroupEnglishName;
                Element = value.Element;
                Description = value.Description;
                Remarks = value.Remarks;
            });

        this.WhenAnyPropertyChanged()
            .Throttle(TimeSpan.FromMilliseconds(400))
            .WhereNotNull()
            .Subscribe(value =>
            {
                source.Zone = value.Zone;
                source.ZoneName = value.ZoneName;
                source.ZoneEnglishName = value.ZoneEnglishName;
                source.Group = value.Group;
                source.GroupName = value.GroupName;
                source.GroupEnglishName = value.GroupEnglishName;
                source.Element = value.Element;
                source.Description = value.Description;
                source.Remarks = value.Remarks;
            });

        #endregion
    }


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
}