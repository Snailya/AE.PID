namespace AE.PID.Client.Core;

public record FunctionLocationDetail(
    string Zone,
    string ZoneName,
    string ZoneEnglishName,
    string Group,
    string GroupName,
    string GroupEnglishName,
    string Element,
    string Description,
    string Remarks,
    string Responsibility)
{
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

    public string Remarks { get; set; } = Remarks;

    /// <summary>
    ///     The responsibility for this function location.
    /// </summary>
    public string Responsibility { get; set; } = Responsibility;
}