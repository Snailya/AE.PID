using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using AE.PID.Client.Core;
using AE.PID.Client.Infrastructure.Extensions;
using AE.PID.UI.Shared;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public class FunctionsViewModel : ViewModelBase
{
    private readonly ReadOnlyObservableCollection<FunctionLocationTreeItemViewModel> _locations;
    private bool _isLoading = true;
    private ProjectViewModel? _project;
    private FunctionLocationTreeItemViewModel? _selectedLocation;

    public ReadOnlyObservableCollection<FunctionLocationTreeItemViewModel> Locations => _locations;

    public FunctionLocationTreeItemViewModel? SelectedLocation
    {
        get => _selectedLocation;
        set => this.RaiseAndSetIfChanged(ref _selectedLocation, value);
    }

    # region -- View Bounded --

    public FunctionKanbanViewModel Kanban { get; }

    # endregion

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    /// <summary>
    ///     The project is used for deciding whether the function selection and synchronization could be chosen.
    /// </summary>
    public ProjectViewModel? Project
    {
        get => _project;
        set => this.RaiseAndSetIfChanged(ref _project, value);
    }

    #region -- Constructors --

    internal FunctionsViewModel()
    {
        // Design
    }

    public FunctionsViewModel(NotificationHelper notificationHelper,
        IFunctionService functionService,
        IFunctionLocationStore functionLocationStore,
        IMaterialLocationStore materialLocationStore)
    {
        // initialize view bounded view models
        Kanban = new FunctionKanbanViewModel(notificationHelper, functionService, functionLocationStore,
            materialLocationStore);

        #region -- Subscriptions --

        functionLocationStore.FunctionLocations.Connect()
            .Transform(x => x.Location)
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
            .TransformToTree<FunctionLocation, ICompoundKey>(x => x.ParentId, Observable.Return(DefaultPredicate))
            .Transform(node => new FunctionLocationTreeItemViewModel(node))
            .SortAndBind(out _locations,
                SortExpressionComparer<FunctionLocationTreeItemViewModel>.Ascending(x => x.NodeName)
                    .ThenBy(x => x.Id))
            .DisposeMany()
            .Subscribe();

        this.WhenValueChanged(x => x.SelectedLocation)
            .WhereNotNull()
            .Subscribe(x => { Kanban.Location = x; });

        #endregion

        return;

        bool DefaultPredicate(Node<FunctionLocation, ICompoundKey> node)
        {
            return node.IsRoot;
        }
    }

    #endregion
}