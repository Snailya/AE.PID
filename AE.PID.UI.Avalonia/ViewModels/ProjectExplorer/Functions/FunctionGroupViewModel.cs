using AE.PID.Client.Core;

namespace AE.PID.Client.UI.Avalonia;

public class FunctionGroupViewModel
{
    public FunctionGroupViewModel(FunctionLocation location)
    {
        FunctionId = location.FunctionId;
        Zone = location.Zone;
        Group = location.Group;
        GroupName = location.GroupName;
        GroupEnglishName = location.GroupEnglishName;
        Description = location.Description;
    }

    public FunctionGroupViewModel()
    {
    }

    public int? FunctionId { get; set; }
    public string Zone { get; set; }
    public string Group { get; set; }
    public string GroupName { get; set; }
    public string GroupEnglishName { get; set; }
    public string Description { get; set; }
}