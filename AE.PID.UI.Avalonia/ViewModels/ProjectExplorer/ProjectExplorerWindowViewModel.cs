using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Client.Core;
using AE.PID.UI.Shared;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public class ProjectExplorerWindowViewModel : WindowViewModelBase
{
    private readonly IFunctionLocationStore _functionLocationStore;
    private readonly IMaterialLocationStore _materialLocationStore;
    private int _viewIndex;

    public int ViewIndex
    {
        get => _viewIndex;
        set => this.RaiseAndSetIfChanged(ref _viewIndex, value);
    }

    protected override void SetupSubscriptions(CompositeDisposable d)
    {
        base.SetupSubscriptions(d);

        // load data if switch tab to function or material 
        this.WhenAnyValue(x => x.ViewIndex)
            .Where(x => x > 0)
            .Take(1)
            .Subscribe(_ =>
            {
                _functionLocationStore.Load();
                _materialLocationStore.Load();
            });
    }

    # region -- View Bounded --

    public ProjectsViewModel Projects { get; }
    public MaterialsViewModel Materials { get; }
    public FunctionsViewModel Functions { get; }

    #endregion

    #region -- Constructors --

    internal ProjectExplorerWindowViewModel()
    {
        // Design
    }

    public ProjectExplorerWindowViewModel(NotificationHelper notificationHelper,
        IProjectService projectService,
        IFunctionService functionService, IMaterialService materialService,
        IProjectLocationStore projectLocationStore, IFunctionLocationStore functionLocationStore,
        IMaterialLocationStore materialLocationStore) : base(notificationHelper,
        NotificationHelper.Routes.ProjectExplorer)
    {
        _functionLocationStore = functionLocationStore;
        _materialLocationStore = materialLocationStore;

        // initialize view bounded view models
        Projects = new ProjectsViewModel(notificationHelper, projectService, projectLocationStore);
        Materials = new MaterialsViewModel(notificationHelper, functionLocationStore, materialLocationStore,
            materialService);
        Functions = new FunctionsViewModel(notificationHelper, functionService, functionLocationStore,
            materialLocationStore);

        // when project changes, propagate to functions because it uses project to decide whether selection function feature is available
        this.WhenAnyValue(x => x.Projects.Project)
            .Subscribe(x =>
            {
                Functions.Project = x;
                Materials.Project = x;
            });
    }

    #endregion
}