using AE.PID.Client.Core;

namespace AE.PID.Client.UI.Avalonia;

public class FunctionGroupViewModel(FunctionLocation location)
{
    /// <summary>
    ///     The id in PDMS for the function.
    /// </summary>
    public int? FunctionId { get; set; } = location.FunctionId;

    /// <summary>
    ///     The zone designation that the function belongs to
    /// </summary>
    public string Zone { get; set; } = location.Zone;

    /// <summary>
    ///     The group designation of the function
    /// </summary>
    public string Group { get; set; } = location.Group;

    /// <summary>
    ///     The name of the group.
    /// </summary>
    public string GroupName { get; set; } = location.GroupName;

    /// <summary>
    ///     The english name of the group.
    /// </summary>
    public string GroupEnglishName { get; set; } = location.GroupEnglishName;

    /// <summary>
    ///     The description of the group.
    /// </summary>
    public string Description { get; set; } = location.Description;
}