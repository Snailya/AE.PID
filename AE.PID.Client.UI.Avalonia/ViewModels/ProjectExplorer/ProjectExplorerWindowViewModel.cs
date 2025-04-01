using System;
using AE.PID.Client.Core;
using AE.PID.Client.UI.Avalonia.Shared;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public class ProjectExplorerWindowViewModel : WindowViewModelBase
{
    private int _viewIndex;

    public int ViewIndex
    {
        get => _viewIndex;
        set => this.RaiseAndSetIfChanged(ref _viewIndex, value);
    }

    # region -- View Bounded --

    public ProjectsViewModel Projects { get; }
    public MaterialsViewModel Materials { get; }
    public FunctionsViewModel Functions { get; }

    #endregion

    #region -- Constructors --

    public ProjectExplorerWindowViewModel(NotificationHelper notificationHelper,
        IProjectService projectService,
        IFunctionService functionService, IMaterialService materialService,
        IProjectLocationStore projectLocationStore, IFunctionLocationStore functionLocationStore,
        IMaterialLocationStore materialLocationStore) : base(notificationHelper,
        NotificationHelper.Routes.ProjectExplorer)
    {
        // initialize view bounded view models
        Projects = new ProjectsViewModel(notificationHelper, projectService, projectLocationStore);
        Materials = new MaterialsViewModel(notificationHelper, functionLocationStore, materialLocationStore,
            materialService);
        Functions = new FunctionsViewModel(notificationHelper, functionService, functionLocationStore,
            materialLocationStore);

        // when project changes, propagate to functions because it uses a project to decide whether selection function feature is available
        this.WhenAnyValue(x => x.Projects.Project)
            .Subscribe(project =>
            {
                Functions.Project = project;
                Materials.Project = project;
            });
    }

    #endregion
}