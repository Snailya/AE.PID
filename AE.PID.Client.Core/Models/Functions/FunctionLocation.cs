using System;
using AE.PID.Core.Interfaces;
using AE.PID.Core.Models;

namespace AE.PID.Client.Core;

/// <summary>
///     A function location is a site designated for implementing a specific function, either physically or logically.
///     A function location can be further subdivided into more locations.
/// </summary>
public record FunctionLocation(
    ICompoundKey Id,
    ICompoundKey ParentId,
    string Name,
    FunctionType Type,
    int? FunctionId,
    string Zone,
    string ZoneName,
    string ZoneEnglishName,
    string Group,
    string GroupName,
    string GroupEnglishName,
    string Element,
    string Description,
    string Remarks,
    string Responsibility,
    bool IsOptional
)
    : ILocation, ITreeNode<ICompoundKey>, IComparable<FunctionLocation>
{
    public int? FunctionId { get; set; } = FunctionId;

    /// <summary>
    ///     The code of the process zone that the location belongs to.
    /// </summary>
    public string Zone { get; set; } = Zone;

    /// <summary>
    ///     The user-friendly name of the zone it belongs to.
    /// </summary>
    public string ZoneName { get; set; } = ZoneName;

    /// <summary>
    ///     The user-friendly english name of the zone it belongs to.
    /// </summary>
    public string ZoneEnglishName { get; set; } = ZoneEnglishName;

    /// <summary>
    ///     The code of the group that the location belongs to.
    ///     Leave empty if it is a process zone
    /// </summary>
    public string Group { get; set; } = Group;

    /// <summary>
    ///     The user-friendly name of the group it belongs to.
    /// </summary>
    public string GroupName { get; set; } = GroupName;

    /// <summary>
    ///     The user-friendly english name of the group it belongs to.
    /// </summary>
    public string GroupEnglishName { get; set; } = GroupEnglishName;

    /// <summary>
    ///     The code of the element that the location belongs to.
    ///     Only has value when it is of type Equipment, Instrument or Function Element.
    /// </summary>
    public string Element { get; set; } = Element;

    /// <summary>
    ///     The description for the location, mapping to Prop.ProcessAreaDescription,
    ///     Prop.FunctionalGroupDescription,Prop.Description
    /// </summary>
    public string Description { get; set; } = Description;

    /// <summary>
    ///     The type for the location.
    /// </summary>
    public FunctionType Type { get; } = Type;

    public string Remarks { get; set; } = Remarks;

    /// <summary>
    ///     The responsibility for this function location.
    /// </summary>
    public string Responsibility { get; set; } = Responsibility;

    /// <summary>
    ///     Whether the location is an optional location, which means no need to display
    /// </summary>
    public bool IsOptional { get; set; } = IsOptional;

    public int CompareTo(FunctionLocation other)
    {
        var zoneCompared = string.Compare(Zone, other.Zone, StringComparison.Ordinal);
        if (zoneCompared != 0) return zoneCompared;

        var groupCompared = string.Compare(Group, other.Group, StringComparison.Ordinal);
        if (groupCompared != 0) return groupCompared;

        var elementCompared = string.Compare(Element, other.Element, StringComparison.Ordinal);
        if (elementCompared != 0) return elementCompared;

        return 0;
    }

    #region -- ITreeNode<CompositeId> --

    public ICompoundKey Id { get; } = Id;

    /// <summary>
    ///     The id that is the logical parent of the location.
    /// </summary>
    public ICompoundKey ParentId { get; set; } = ParentId;

    public string Name { get; } = Name;

    // public string NodeName => Name;

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