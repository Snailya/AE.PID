using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using AE.PID.Core.Models;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using AE.PID.Visio.Core.Models.Projects;
using AE.PID.Visio.UI.Avalonia.Services;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class FunctionKanbanViewModel : ViewModelBase
{
    private readonly ReadOnlyObservableCollection<FunctionGroupViewModel> _groups;
    private readonly ReadOnlyObservableCollection<MaterialLocationViewModel> _materials;
    private readonly ObservableAsPropertyHelper<Project?> _project = ObservableAsPropertyHelper<Project?>.Default();
    private readonly ObservableAsPropertyHelper<FunctionLocationPropertiesViewModel> _properties;
    private DateTime? _lastSynced;
    private FunctionLocationViewModel _selectedLocation;

    public DateTime? LastSynced
    {
        get => _lastSynced;
        set => this.RaiseAndSetIfChanged(ref _lastSynced, value);
    }

    /// <summary>
    ///     The basic information for the function location. Parts of these information could be synchronized from the server
    ///     by selecting the target function in PDMS.
    /// </summary>
    public FunctionLocationPropertiesViewModel Properties => _properties.Value;

    /// <summary>
    ///     The bill of materials belongs to this function location if the location is either Function Zone or Function Group.
    /// </summary>
    public ReadOnlyObservableCollection<MaterialLocationViewModel> Materials => _materials;

    /// <summary>
    ///     The current location that the Kanban is presenting.
    /// </summary>
    public FunctionLocationViewModel SelectedLocation
    {
        get => _selectedLocation;
        set => this.RaiseAndSetIfChanged(ref _selectedLocation, value);
    }

    /// <summary>
    ///     The function groups under the function location if the location is the Function Zone.
    /// </summary>
    public ReadOnlyObservableCollection<FunctionGroupViewModel> Groups => _groups;

    /// <summary>
    ///     The project info, which is not presented in the View, but to provide information when calling some of the APIs.
    /// </summary>
    private Project? Project => _project?.Value;

    private static bool IsDescendant(IObservableCache<FunctionLocation, CompositeId> cache, FunctionLocation obj,
        CompositeId parentId)
    {
        while (true)
        {
            // Recursive check: If the object itself is a direct child
            if (obj.ParentId.Equals(parentId)) return true;

            // Check if the object's parent is a descendant of the given parentId
            if (!cache.Lookup(obj.ParentId).HasValue) return false;
            var parent = cache.Lookup(obj.ParentId).Value;
            obj = parent;
        }
    }

    #region -- Commands --

    public ReactiveCommand<FunctionType?, Unit> SelectFunction { get; }
    public ReactiveCommand<Unit, Unit> SyncFunctionGroups { get; }

    #endregion

    #region -- Interactions --

    /// <summary>
    ///     When user want to synchronize basic information for the function from server, a prompted window should be
    ///     displayed, this interaction is used to telling the hosted window to open that prompt window.
    /// </summary>
    public Interaction<SelectFunctionViewModel?, FunctionViewModel?> ShowSelectFunctionDialog { get; } =
        new();

    /// <summary>
    ///     Before actually syn function groups to the server, the user should check and confirm the change as the change might
    ///     lead to unrecoverable changes in the server.
    /// </summary>
    public Interaction<ConfirmSyncFunctionGroupsViewModel?, Function[]?> ShowSyncFunctionGroupsDialog { get; } = new();

    #endregion

    #region -- Constructors --

    public FunctionKanbanViewModel(IProjectStore projectStore, IFunctionService functionService,
        IFunctionLocationStore fLocStore,
        IMaterialLocationStore mLocStore, NotifyService notifyService)
    {
        #region -- Commands --

        // for the function zone, the selection is allowed when there is a project specified
        // but for function group, it could happen any time
        var canSelect = projectStore.Project.Select(x => x.Value)
            .CombineLatest(this.WhenAnyValue(x => x.SelectedLocation.Source.Type))
            .Select(x =>
            {
                var (project, type) = x;
                if (project != null && type == FunctionType.ProcessZone) return true;
                if (type == FunctionType.FunctionGroup) return true;
                return false;
            });
        SelectFunction =
            ReactiveCommand.CreateFromTask<FunctionType?>(
                async type =>
                {
                    if (type == null) throw new ArgumentNullException(nameof(type));

                    // build the view model based on the function type, for process zone, return the function zones under the project
                    // for function group, return the standard function groups
                    SelectFunctionViewModel? viewModel = null;
                    if (type == FunctionType.ProcessZone)
                        viewModel = new SelectFunctionViewModel(functionService, Project!.Id);
                    else if (type == FunctionType.FunctionGroup)
                        viewModel = new SelectFunctionViewModel(functionService);

                    // prompt the window for selection
                    var dialogResult = await ShowSelectFunctionDialog.Handle(viewModel);
                    if (dialogResult == null) return;

                    // if there is a user selection, try to update the function location information
                    fLocStore.Update(SelectedLocation.Source.Id, dialogResult.Source);
                }, canSelect);
        SelectFunction.ThrownExceptions
            .Subscribe(v => { notifyService.Error("选择失败", v!.Message); });

        SyncFunctionGroups = ReactiveCommand.CreateFromTask(async () =>
        {
            var viewModel = new ConfirmSyncFunctionGroupsViewModel(functionService, fLocStore, Project!.Id,
                SelectedLocation.Source.FunctionId, SelectedLocation.Id);
            var dialogResult = await ShowSyncFunctionGroupsDialog.Handle(viewModel);
            if (dialogResult == null) return;

            // if there is a confirmation from user, try to do synchronization
            await functionService.SyncFunctionGroupsAsync(Project!.Id, SelectedLocation.Source.FunctionId,
                dialogResult);
        });
        SyncFunctionGroups.ThrownExceptions
            .Subscribe(v => { notifyService.Error("同步功能组失败", v!.Message); });

        #endregion

        #region -- Subscriptions --

        projectStore.Project
            .Do(x =>
            {
                if (!x.IsSuccess)
                    notifyService.Error("加载项目信息失败", x.Exception!.Message);
            })
            .Select(x => x.Value)
            .ToProperty(this, x => x.Project, out _project, scheduler: RxApp.MainThreadScheduler);

        var functionObservable = fLocStore.FunctionLocations.Connect();

        this.WhenAnyValue(x => x.SelectedLocation.Id)
            .Select(id => functionObservable.WatchValue(id))
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(x => new FunctionLocationPropertiesViewModel(x))
            .ToProperty(this, x => x.Properties, out _properties);

        var groupFilter = this.WhenAnyValue(x => x.SelectedLocation.Id)
            .Select<CompositeId?, Func<FunctionLocation, bool>>(x => loc => loc.ParentId.Equals(x));
        functionObservable
            .Filter(groupFilter)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Transform(x => new FunctionGroupViewModel(x))
            .Bind(out _groups)
            .Subscribe();

        var materialFilter = this.WhenAnyValue(x => x.SelectedLocation.Id)
            .Select<CompositeId, Func<FunctionLocation, bool>>(v =>
                location => IsDescendant(fLocStore.FunctionLocations, location, v) && (int)location.Type > 2);
        functionObservable
            .ObserveOn(RxApp.MainThreadScheduler)
            .Filter(materialFilter)
            .LeftJoin(mLocStore.MaterialLocations.Connect(), x => x.LocationId,
                (left, right) =>
                    new { Function = left, Material = right })
            .Filter(x => x.Material.HasValue)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Transform(x => new MaterialLocationViewModel(x.Material.Value, x.Function))
            .SortAndBind(out _materials,
                SortExpressionComparer<MaterialLocationViewModel>.Ascending(x => x.ProcessArea)
                    .ThenByAscending(x => x.FunctionalGroup)
                    .ThenByAscending(x => x.FunctionalElement)
            )
            .Subscribe();

        #endregion
    }

    internal FunctionKanbanViewModel()
    {
        // Design
    }

    #endregion
}