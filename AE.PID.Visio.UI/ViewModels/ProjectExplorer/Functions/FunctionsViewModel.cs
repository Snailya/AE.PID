using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
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

    public FunctionsViewModel(IProjectStore projectStore, IFunctionService functionService,
        IFunctionLocationStore functionLocationStore,
        IMaterialLocationStore mLocStore, NotifyService notifyService)
    {
        Kanban = new FunctionKanbanViewModel(projectStore, functionService, functionLocationStore, mLocStore,
            notifyService);

        #region -- Subscriptions --

        functionLocationStore.FunctionLocations.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(_ => { IsLoading = true; })
            .ObserveOn(RxApp.TaskpoolScheduler)
            .TransformToTree<FunctionLocation, CompositeId>(x => x.ParentId, Observable.Return(DefaultPredicate))
            .Transform(node => new FunctionLocationViewModel(node))
            .ObserveOn(RxApp.MainThreadScheduler)
            .SortAndBind(out _locations,
                SortExpressionComparer<FunctionLocationViewModel>.Ascending(x => x.Name).ThenBy(x => x.Id.ShapeId))
            .DisposeMany()
            .Do(x => IsLoading = false)
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