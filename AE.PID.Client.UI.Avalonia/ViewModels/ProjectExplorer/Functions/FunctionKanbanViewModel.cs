using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using AE.PID.Client.Core;
using AE.PID.Client.UI.Avalonia.Shared;
using AE.PID.Core;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public class FunctionKanbanViewModel : ViewModelBase
{
    private readonly ReadOnlyObservableCollection<FunctionGroupViewModel> _groups;
    private readonly ReadOnlyObservableCollection<MaterialLocationViewModel> _materials;

    private readonly ObservableAsPropertyHelper<FunctionLocationPropertiesViewModel> _properties =
        ObservableAsPropertyHelper<FunctionLocationPropertiesViewModel>.Default();

    private DateTime? _lastSynced;
    private FunctionLocationTreeItemViewModel? _location;
    private Project? _project;

    #region -- Constructors --

    public FunctionKanbanViewModel(NotificationHelper notificationHelper, IFunctionService functionService,
        IFunctionLocationStore fLocStore,
        IMaterialLocationStore mLocStore)
    {
        #region -- Commands --

        // for the function zone, the selection is allowed when there is a project specified
        // but for a function group, it could happen any time
        var canSelect = this.WhenAnyValue(x => x.Project, x => x.Properties.FunctionType)
            // switch to UI thread
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(x => x is { Item1: not null, Item2: FunctionType.ProcessZone } ||
                         x.Item2 == FunctionType.FunctionGroup);

        SelectFunction =
            ReactiveCommand.CreateFromTask(
                async kanban =>
                {
                    var type = Properties.FunctionType;

                    // build the view model based on the function type, for process zone, return the function zones under the project
                    // for function group, return the standard function groups
                    var viewModel = type switch
                    {
                        FunctionType.ProcessZone => new SelectFunctionViewModel(functionService, Project!.Id),
                        FunctionType.FunctionGroup => new SelectFunctionViewModel(functionService),
                        _ => null
                    };

                    // prompt the window for selection
                    var dialogResult = await ShowSelectFunctionDialog.Handle(viewModel);
                    if (dialogResult == null) return;

                    Properties.Source.FunctionId = dialogResult.Id;

                    // update the properties
                    // there are two circumstances, the first is that the function group field is totally empty, then use the default function group code as input
                    // 2025.02.13: use the default <功能组> to decide whether to overwrite the function group label or fix its code
                    if (string.IsNullOrEmpty(Properties.Group) || Properties.Group == DefaultValueDict.FunctionGroup)
                    {
                        Properties.Group = dialogResult.Code;
                    }
                    else // the second circumstance is that the user has already specified a number for the group, then strip it from the field and replace the prefix only
                    {
                        var number = Regex.Match(Properties.Group, @"(\d+)$").Value;
                        var prefix = Regex.Match(dialogResult.Code, @"(^[A-Za-z]+)").Value;
                        Properties.Group = prefix + number;
                    }

                    Properties.GroupName = dialogResult.Name;
                    Properties.GroupEnglishName = dialogResult.EnglishName;
                }, canSelect);
        SelectFunction.ThrownExceptions
            .Subscribe(v => { notificationHelper.Error("选择失败", v!.Message); });

        var canSync = this.WhenAnyValue(x => x.Project, x => x.Properties.FunctionId,
                (project, functionId) => project != null && project.Id != 0 && functionId != null)
            // switch to UI thread
            .ObserveOn(RxApp.MainThreadScheduler);
        SyncFunctionGroups = ReactiveCommand.CreateFromTask(async () =>
        {
            var viewModel = new ConfirmSyncFunctionGroupsViewModel(functionService, fLocStore, Project!.Id,
                Properties.FunctionId!.Value, Location.Id);
            var dialogResult = await ShowSyncFunctionGroupsDialog.Handle(viewModel);
            if (dialogResult == null) return;

            // if there is a confirmation from user, try to do synchronization
            await functionService.SyncFunctionGroupsAsync(Project!.Id, Properties.Source.FunctionId!.Value,
                dialogResult);
        }, canSync);
        SyncFunctionGroups.ThrownExceptions
            .Subscribe(v => { notificationHelper.Error("同步功能组失败", v!.Message); });

        #endregion

        #region -- Subscriptions --

        var functionObservable = fLocStore.FunctionLocations.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Transform(x => x.Location);

        this.WhenAnyValue(x => x.Location)
            .WhereNotNull()
            .Select(x => x.Id)
            .Select(id => functionObservable.WatchValue(id))
            .Switch()
            .Select(x => new FunctionLocationPropertiesViewModel(x))
            .ToProperty(this, x => x.Properties, out _properties);

        var groupFilter = this.WhenAnyValue(x => x.Location.Id)
            .DistinctUntilChanged()
            .Select<ICompoundKey, Func<FunctionLocation, bool>>(x => loc => loc.ParentId!.Equals(x));
        functionObservable
            .Filter(groupFilter)
            .Transform(x => new FunctionGroupViewModel(x))
            .Bind(out _groups)
            .Subscribe();

        var materialFilter = this.WhenAnyValue(x => x.Location)
            .WhereNotNull()
            .Select<FunctionLocationTreeItemViewModel, Func<FunctionLocation, bool>>(v =>
                x => v.Flatten().Where(i => (int)i.Type > 2).Select(i => i.Id).Contains(x.Id));
        functionObservable
            .Filter(materialFilter)
            .LeftJoin(mLocStore.MaterialLocations.Connect(), x => x.Location.Id,
                (left, right) =>
                    new { Function = left, Material = right })
            .Filter(x => x.Material.HasValue)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Transform(x =>
                new MaterialLocationViewModel(x.Material.Value.Location, x.Function, x.Material.Value.Material))
            .SortAndBind(out _materials,
                SortExpressionComparer<MaterialLocationViewModel>.Ascending(x => x.ProcessArea)
                    .ThenByAscending(x => x.FunctionalGroup)
                    .ThenByAscending(x => x.FunctionalElement)
            )
            .Subscribe();

        // 2025.04.07：传递数量变化
        var observeChange = _materials.ToObservableChangeSet(t => t.Id);

        observeChange
            .WhenAnyPropertyChanged(nameof(MaterialLocationViewModel.Quantity),
                nameof(MaterialLocationViewModel.MaterialCode))
            .WhereNotNull()
            .Select(x => x.MaterialSource with { Quantity = x.Quantity, Code = x.MaterialCode })
            .Subscribe(x => mLocStore.Update([x]));
        observeChange.WhenAnyPropertyChanged(nameof(MaterialLocationViewModel.Description),
                nameof(MaterialLocationViewModel.Remarks))
            .WhereNotNull()
            .Select(x => x.FunctionSource! with { Description = x.Description, Remarks = x.Remarks })
            .Subscribe(x => fLocStore.Update([x]));

        // 2025.03.28: 使用WhenAnyValue(x=>x.Properties)仅在Properties指向新的地址时发射通知，也就是Properties被重新赋值，对应于Location变化。而当Properties.SubProperty变化时，不会发出新的通知。
        // 当Properties被重新设置后，开始观察Properties的属性变化。
        this.WhenAnyValue(x => x.Properties)
            .WhereNotNull()
            .SelectMany(props => props.WhenAnyPropertyChanged())
            .WhereNotNull()
            .Select(x => Properties.Source with
            {
                FunctionId = x.FunctionId, Zone = x.Zone, ZoneName = x.ZoneName, Group = x.Group,
                GroupName = x.GroupName, GroupEnglishName = x.GroupEnglishName, Element = x.Element,
                Description = x.Description, Remarks = x.Remarks, UnitMultiplier = x.UnitMultiplier
            })
            .Subscribe(x => { fLocStore.Update([x]); });

        #endregion
    }

    #endregion

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
    public FunctionLocationTreeItemViewModel? Location
    {
        get => _location;
        set
        {
            this.RaiseAndSetIfChanged(ref _location, value);
            this.RaisePropertyChanged();
        }
    }

    /// <summary>
    ///     The function groups under the function location if the location is the Function Zone.
    /// </summary>
    public ReadOnlyObservableCollection<FunctionGroupViewModel> Groups => _groups;

    /// <summary>
    ///     The project info, which is not presented in the View, but to provide information when calling some of the APIs.
    /// </summary>
    private Project? Project
    {
        get => _project;
        set => this.RaiseAndSetIfChanged(ref _project, value);
    }

    #region -- Commands --

    public ReactiveCommand<Unit, Unit> SelectFunction { get; }
    public ReactiveCommand<Unit, Unit> SyncFunctionGroups { get; }

    #endregion

    #region -- Interactions --

    /// <summary>
    ///     When user wants to synchronize basic information for the function from server, a prompted window should be
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
}