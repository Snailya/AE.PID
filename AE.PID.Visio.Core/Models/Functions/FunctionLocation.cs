using AE.PID.Core.Interfaces;
using AE.PID.Core.Models;
using DynamicData.Binding;

namespace AE.PID.Visio.Core.Models;

/// <summary>
/// A function location is a site designated for implementing a specific function, either physically or logically.
/// A function location can be further subdivided into more locations.
/// </summary>
/// <param name="id"></param>
/// <param name="type"></param>
public class FunctionLocation(CompositeId id, FunctionType type)
    : AbstractNotifyPropertyChanged, ITreeNode<CompositeId>
{
    private string _description = string.Empty;
    private string _element = string.Empty;
    private int _functionId;
    private string _group = string.Empty;
    private string _groupEnglishName = string.Empty;
    private string _groupName = string.Empty;
    private string _name = string.Empty;
    private CompositeId _parentId = new();
    private string _remarks = string.Empty;
    private string _responsibility = string.Empty;
    private string _zone = string.Empty;
    private string _zoneEnglishName = string.Empty;
    private string _zoneName = string.Empty;

    /// <summary>
    ///     The code of the process zone that the location belongs to.
    /// </summary>
    public string Zone
    {
        get => _zone;
        set => SetAndRaise(ref _zone, value);
    }

    /// <summary>
    ///     The user-friendly name of the zone it belongs to.
    /// </summary>
    public string ZoneName
    {
        get => _zoneName;
        set => SetAndRaise(ref _zoneName, value);
    }

    /// <summary>
    ///     The user-friendly english name of the zone it belongs to.
    /// </summary>
    public string ZoneEnglishName
    {
        get => _zoneEnglishName;
        set => SetAndRaise(ref _zoneEnglishName, value);
    }

    /// <summary>
    ///     The code of the group that the location belongs to.
    ///     Leave empty if it is a process zone
    /// </summary>
    public string Group
    {
        get => _group;
        set => SetAndRaise(ref _group, value);
    }

    /// <summary>
    ///     The user-friendly name of the group it belongs to.
    /// </summary>
    public string GroupName
    {
        get => _groupName;
        set => SetAndRaise(ref _groupName, value);
    }

    /// <summary>
    ///     The user-friendly english name of the group it belongs to.
    /// </summary>
    public string GroupEnglishName
    {
        get => _groupEnglishName;
        set => SetAndRaise(ref _groupEnglishName, value);
    }

    /// <summary>
    ///     The code of the element that the location belongs to.
    ///     Only has value when it is of type Equipment, Instrument or Function Element.
    /// </summary>
    public string Element
    {
        get => _element;
        set => SetAndRaise(ref _element, value);
    }

    /// <summary>
    ///     The description for the location, mapping to Prop.ProcessAreaDescription,
    ///     Prop.FunctionalGroupDescription,Prop.Description
    /// </summary>
    public string Description
    {
        get => _description;
        set => SetAndRaise(ref _description, value);
    }

    /// <summary>
    ///     The type for the location.
    /// </summary>
    public FunctionType Type { get; } = type;

    /// <summary>
    ///     The name for the location, mapping to Prop.ProcessAreaName, Prop.FunctionalGroupName, Prop.SubClass
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetAndRaise(ref _name, value);
    }

    public string Remarks
    {
        get => _remarks;
        set => SetAndRaise(ref _remarks, value);
    }

    /// <summary>
    ///     The PDMS related id.
    /// </summary>
    public int FunctionId
    {
        get => _functionId;
        set => SetAndRaise(ref _functionId, value);
    }

    /// <summary>
    ///     The responsibility for this function location.
    /// </summary>
    public string Responsibility
    {
        get => _responsibility;
        set => SetAndRaise(ref _responsibility, value);
    }

    #region -- ITreeNode<CompositeId> --

    /// <summary>
    ///     The id that used to find the shape proxy in Visio drawing.
    /// </summary>
    public CompositeId Id { get; } = id;

    /// <summary>
    ///     The id that is the logical parent of the location.
    /// </summary>
    public CompositeId ParentId
    {
        get => _parentId;
        set => SetAndRaise(ref _parentId, value);
    }

    public string NodeName
    {
        get
        {
            return Type switch
            {
                FunctionType.ProcessZone => Zone,
                FunctionType.FunctionGroup => Group,
                FunctionType.FunctionUnit => "单元",
                FunctionType.Equipment => Element,
                FunctionType.Instrument => Element,
                FunctionType.FunctionElement => Element,
                FunctionType.External => Responsibility,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    #endregion
}