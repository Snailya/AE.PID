using AE.PID.Visio.Core.Models;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class FunctionGroupViewModel(FunctionLocation location)
{
    public string Zone { get; set; } = location.Zone;

    public string Group { get; set; } = location.Group;

    public string GroupName { get; set; } = location.GroupName;
    public string GroupEnglishName { get; set; } = location.GroupEnglishName;

    public string Description { get; set; } = location.Description;
}