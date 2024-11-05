using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.UI.Avalonia.Services;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class ProjectExplorerWindowViewModel : WindowViewModelBase
{
    private readonly IFunctionLocationStore _functionLocationStore;
    private readonly IMaterialLocationStore _materialLocationStore;
    private int _viewIndex;
    public ProjectsViewModel Projects { get; set; }
    public MaterialsViewModel Materials { get; set; }
    public FunctionsViewModel Functions { get; set; }

    public int ViewIndex
    {
        get => _viewIndex;
        set => this.RaiseAndSetIfChanged(ref _viewIndex, value);
    }

    protected override void SetupSubscriptions(CompositeDisposable d)
    {
        base.SetupSubscriptions(d);

        // load data if switch tab to function or material 
        this.WhenAnyValue(x => x.ViewIndex).Where(x => x > 0).Take(1).Subscribe(_ =>
        {
            _functionLocationStore.Load();
            _materialLocationStore.Load();
        });
    }

    #region -- Constructors --

    internal ProjectExplorerWindowViewModel()
    {
        // Design
    }

    public ProjectExplorerWindowViewModel(NotificationHelper notificationHelper,
        IProjectService projectService,
        IFunctionService functionService, IMaterialService materialService,
        IProjectStore projectStore, IFunctionLocationStore functionLocationStore,
        IMaterialLocationStore materialLocationStore) : base(notificationHelper,
        NotificationHelper.Routes.ProjectExplorer)
    {
        _functionLocationStore = functionLocationStore;
        _materialLocationStore = materialLocationStore;

        Projects = new ProjectsViewModel(notificationHelper, projectService, projectStore);
        Materials = new MaterialsViewModel(notificationHelper, projectStore, functionLocationStore, materialLocationStore,
            materialService);
        Functions = new FunctionsViewModel(notificationHelper,projectStore, functionService, functionLocationStore, materialLocationStore);

    }

    #endregion
}