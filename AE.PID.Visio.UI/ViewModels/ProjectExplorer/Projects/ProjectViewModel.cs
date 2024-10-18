using AE.PID.Visio.Core.Models.Projects;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class ProjectViewModel : ViewModelBase
{
    public ProjectViewModel(Project project)
    {
        Id = project.Id;
        Code = project.Code;
        Name = project.Name;
        FamilyName = project.FamilyName;
    }

    public int Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string FamilyName { get; set; }
}