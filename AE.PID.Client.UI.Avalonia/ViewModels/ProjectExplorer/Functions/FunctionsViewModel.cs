using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using AE.PID.Client.Core;
using AE.PID.Client.UI.Avalonia.Shared;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Splat;

namespace AE.PID.Client.UI.Avalonia;

public class FunctionsViewModel : ViewModelBase
{
    private readonly ReadOnlyObservableCollection<FunctionLocationTreeItemViewModel> _locations;
    private bool _isLoading = true;
    private ProjectViewModel? _project;
    private FunctionLocationTreeItemViewModel? _selectedLocation;
    private bool _showNotSelectedItems;

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

    public bool ShowNotSelectedItems
    {
        get => _showNotSelectedItems;
        set => this.RaiseAndSetIfChanged(ref _showNotSelectedItems, value);
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

        var includeFilter = this.WhenAnyValue(x => x.ShowNotSelectedItems)
            .Select<bool, Func<FunctionLocation, bool>>(x => loc => x || loc.IsIncludeInProject == true);

        functionLocationStore.FunctionLocations.Connect()
            .Transform(x => x.Location)
            // switch to the UI thread to handle view models
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(changes =>
            {
                if (changes.Any(x => x.Current.ParentId == null))
                    this.Log().Debug(
                        "The parent id is null for some of the function location item, please check from IDE. The exceptional data will no be fitlered to ensure the TransformToTree method works normally.");
            })
            .Filter(includeFilter)
            .Filter(x => x.ParentId != null)
            .TransformToTree<FunctionLocation, ICompoundKey>(x => x.ParentId!, Observable.Return(DefaultPredicate))
            .Transform(node => new FunctionLocationTreeItemViewModel(node))
            .SortAndBind(out _locations,
                SortExpressionComparer<FunctionLocationTreeItemViewModel>.Ascending(x => x.NodeName)
                    .ThenBy(x => x.Id))
            .DisposeMany()
            .Do(_ => { IsLoading = false; })
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