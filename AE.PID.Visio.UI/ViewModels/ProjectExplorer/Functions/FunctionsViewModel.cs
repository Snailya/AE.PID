using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using AE.PID.Visio.Shared.Extensions;
using AE.PID.Visio.UI.Avalonia.Services;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class FunctionsViewModel : ViewModelBase
{
    private readonly ReadOnlyObservableCollection<FunctionLocationViewModel> _locations;
    private bool _isLoading = true;
    private FunctionLocationViewModel? _selectedLocation;

    public ReadOnlyObservableCollection<FunctionLocationViewModel> Locations => _locations;

    public FunctionLocationViewModel? SelectedLocation
    {
        get => _selectedLocation;
        set => this.RaiseAndSetIfChanged(ref _selectedLocation, value);
    }

    public FunctionKanbanViewModel Kanban { get; set; }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    #region -- Constructors --

    internal FunctionsViewModel()
    {
        // Design
    }

    public FunctionsViewModel(NotificationHelper notificationHelper, IProjectStore projectStore,
        IFunctionService functionService,
        IFunctionLocationStore functionLocationStore,
        IMaterialLocationStore materialLocationStore)
    {
#if DEBUG
        DebugExt.Log("Initializing FunctionsViewModel",null, nameof(FunctionsViewModel));
#endif

        Kanban = new FunctionKanbanViewModel(notificationHelper, projectStore, functionService, functionLocationStore,
            materialLocationStore);

        #region -- Subscriptions --

        functionLocationStore.FunctionLocations.Connect()
            // auto refresh here is to trigger the update of the down stream operator if there are property change but no structure change
            .AutoRefresh()
#if DEBUG
            .OnItemAdded(x => DebugExt.Log("FunctionLocations.OnItemAdded", x.Id, nameof(FunctionsViewModel)))
            .OnItemUpdated((cur, prev, _) =>
                DebugExt.Log("FunctionLocations.OnItemUpdated", cur.Id, nameof(FunctionsViewModel)))
            .OnItemRefreshed(x => DebugExt.Log("FunctionLocations.OnItemRefreshed", x.Id, nameof(FunctionsViewModel)))
            .OnItemRemoved(x => DebugExt.Log("FunctionLocations.OnItemRemoved", x.Id, nameof(FunctionsViewModel)))
#endif
            // switch to the UI thread to handle view models
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(x => { IsLoading = false; })
            .TransformToTree<FunctionLocation, CompositeId>(x => x.ParentId, Observable.Return(DefaultPredicate))
            .Transform(node => new FunctionLocationViewModel(node))
            .SortAndBind(out _locations,
                SortExpressionComparer<FunctionLocationViewModel>.Ascending(x => x.Name).ThenBy(x => x.Id.ShapeId))
            .DisposeMany()
            .Subscribe();

        this.WhenValueChanged(x => x.SelectedLocation)
            .WhereNotNull()
            .Subscribe(v => { Kanban.SelectedLocation = v; });

        #endregion

        return;

        bool DefaultPredicate(Node<FunctionLocation, CompositeId> node)
        {
            return node.IsRoot;
        }
    }

    #endregion
}