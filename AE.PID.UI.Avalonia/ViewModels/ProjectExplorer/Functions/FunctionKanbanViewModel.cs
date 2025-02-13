using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using AE.PID.Client.Core;
using AE.PID.Core.Models;
using AE.PID.UI.Avalonia;
using AE.PID.UI.Shared;
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
    private FunctionLocationTreeItemViewModel _location;
    private Project? _project;

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
    public FunctionLocationTreeItemViewModel Location
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

    private static bool IsDescendant(IObservableCache<FunctionLocation, ICompoundKey> cache, FunctionLocation obj,
        ICompoundKey parentId)
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

    public ReactiveCommand<Unit, Unit> SelectFunction { get; }
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

    public FunctionKanbanViewModel(NotificationHelper notificationHelper, IFunctionService functionService,
        IFunctionLocationStore fLocStore,
        IMaterialLocationStore mLocStore)
    {
        #region -- Commands --

        // for the function zone, the selection is allowed when there is a project specified
        // but for function group, it could happen any time
        var canSelect = this.WhenAnyValue(x => x.Project)
            // switch to UI thread
            .ObserveOn(RxApp.MainThreadScheduler)
            .CombineLatest(this.WhenAnyValue(x => x.Properties.FunctionType))
            .Select(x =>
            {
                var (project, type) = x;
                if (project != null && type == FunctionType.ProcessZone) return true;
                if (type == FunctionType.FunctionGroup) return true;
                return false;
            });
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
                    // there are two circumstance, the first is that the function group field is totally empty, then use the default function group code as input
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
            .Select<ICompoundKey, Func<FunctionLocation, bool>>(x => loc => loc.ParentId.Equals(x));
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

        // observe the viewmodel change and back to service
        this.WhenAnyValue(x => x.Properties.FunctionId,
                x => x.Properties.Zone, x => x.Properties.ZoneName, x => x.Properties.ZoneEnglishName,
                x => x.Properties.Group, x => x.Properties.GroupName, x => x.Properties.GroupEnglishName,
                x => x.Properties.Element,
                x => x.Properties.Description, x => x.Properties.Remarks,
                (functionId, zone, zoneName, zoneEnglishName, group, groupName, groupEnglishName, element, description,
                        remarks) =>
                    new
                    {
                        functionId, zone, zoneName, zoneEnglishName, group, groupName, groupEnglishName, element,
                        description, remarks
                    })
            .Select(x => Properties.Source with
            {
                FunctionId = x.functionId, Zone = x.zone, ZoneName = x.zoneName, Group = x.group,
                GroupName = x.groupName, GroupEnglishName = x.groupEnglishName, Element = x.element,
                Description = x.description, Remarks = x.remarks
            })
            .Subscribe(x => fLocStore.Update([x]));

        #endregion
    }

    internal FunctionKanbanViewModel()
    {
        // Design
    }

    #endregion
}

public static class TreeExtensions
{
    public static IEnumerable<FunctionLocationTreeItemViewModel> Flatten(this FunctionLocationTreeItemViewModel node)
    {
        return node.Inferiors.Any()
            ? new[] { node }.Concat(node.Inferiors.SelectMany(child => child.Flatten())) // 使用自身，结合子节点递归展平
            : [node];
    }

    public static IEnumerable<FunctionLocationTreeItemViewModel> Flatten<TObject, TKey>(
        this IEnumerable<FunctionLocationTreeItemViewModel> nodes)
    {
        return nodes.SelectMany(node => node.Flatten());
    }
}