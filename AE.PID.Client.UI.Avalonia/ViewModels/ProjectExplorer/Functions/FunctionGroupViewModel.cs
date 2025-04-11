using AE.PID.Client.Core;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public class FunctionGroupViewModel(FunctionLocation location) : ReactiveObject
{
    private string _description = location.Description;
    private string _group = location.Group;
    private string _groupEnglishName = location.GroupEnglishName;
    private string _groupName = location.GroupName;
    private string _remarks = location.Remarks;
    public FunctionLocation Source { get; } = location;

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
    public string Group
    {
        get => _group;
        set => this.RaiseAndSetIfChanged(ref _group, value);
    }

    /// <summary>
    ///     The name of the group.
    /// </summary>
    public string GroupName
    {
        get => _groupName;
        set => this.RaiseAndSetIfChanged(ref _groupName, value);
    }

    /// <summary>
    ///     The english name of the group.
    /// </summary>
    public string GroupEnglishName
    {
        get => _groupEnglishName;
        set => this.RaiseAndSetIfChanged(ref _groupEnglishName, value);
    }

    /// <summary>
    ///     The description of the group.
    /// </summary>
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
}