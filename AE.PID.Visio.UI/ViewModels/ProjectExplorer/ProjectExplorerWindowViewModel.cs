using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.UI.Avalonia.Services;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class ProjectExplorerWindowViewModel : WindowViewModelBase
{
    public ProjectsViewModel Projects { get; set; }
    public MaterialsViewModel Materials { get; set; }
    public FunctionsViewModel Functions { get; set; }

    #region -- Constructors --

    internal ProjectExplorerWindowViewModel()
    {
        // Design
    }

    public ProjectExplorerWindowViewModel(NotifyService notifyService,
        IProjectService projectService,
        IFunctionService functionService, IMaterialService materialService,
        IProjectStore projectStore, IFunctionLocationStore functionLocationStore,
        IMaterialLocationStore materialLocationStore) : base(notifyService,
        NotifyService.Routes.ProjectExplorer)
    {
        Projects = new ProjectsViewModel(notifyService, projectService, projectStore);
        Materials = new MaterialsViewModel(notifyService, functionLocationStore, materialLocationStore,
            materialService);
        Functions = new FunctionsViewModel(projectStore, functionService, functionLocationStore, materialLocationStore,
            notifyService);
    }

    #endregion
}