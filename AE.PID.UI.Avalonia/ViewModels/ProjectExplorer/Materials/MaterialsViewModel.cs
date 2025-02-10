using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using AE.PID.Client.Infrastructure.Extensions;
using AE.PID.Core.Models;
using AE.PID.UI.Avalonia;
using AE.PID.UI.Avalonia.Models;
using AE.PID.UI.Shared;
using Avalonia.Collections;
using Avalonia.Platform.Storage;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public class MaterialsViewModel : ViewModelBase
{
    private readonly ObservableCollectionExtended<MaterialLocationViewModel> _locations = [];
    private readonly ReadOnlyObservableCollection<MaterialLocationViewModel> _selectedLocations;

    private ValueTuple<string, string>? _clipboard;

    private bool _isLoading = true;
    private bool _isMaterialVisible;
    private MaterialViewModel? _material;
    private ProjectViewModel? _project;
    private string? _searchText;
    private MaterialLocationViewModel? _selectedLocation;
    public ObservableCollection<GroupDescriptionViewModel> GroupDescriptions { get; set; } = [];

    public string? SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public ValueTuple<string, string>? Clipboard
    {
        get => _clipboard;
        set => this.RaiseAndSetIfChanged(ref _clipboard, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public MaterialViewModel? Material
    {
        get => _material;
        set => this.RaiseAndSetIfChanged(ref _material, value);
    }

    public MaterialLocationViewModel? SelectedLocation
    {
        get => _selectedLocation;
        set => this.RaiseAndSetIfChanged(ref _selectedLocation, value);
    }

    public ReadOnlyObservableCollection<MaterialLocationViewModel> SelectedLocations => _selectedLocations;

    public DataGridCollectionView Locations { get; }

    public bool IsMaterialVisible
    {
        get => _isMaterialVisible;
        set => this.RaiseAndSetIfChanged(ref _isMaterialVisible, value);
    }

    private void ResetIsEnabled(MaterialLocationViewModel x)
    {
        if (SelectedLocations.Any()) return;
        foreach (var vm in _locations)
            vm.IsEnabled = true;
    }

    private void UpdateIsEnabled(MaterialLocationViewModel x)
    {
        if (SelectedLocations.Count != 1) return;
        foreach (var vm in _locations)
            vm.IsEnabled = x.MaterialType == vm.MaterialType;
    }

    #region -- Interactions --

    public Interaction<SyncMaterialsViewModel, Unit> ShowSyncMaterialsDialog { get; } = new();
    public Interaction<SelectMaterialWindowViewModel?, MaterialViewModel?> ShowSelectMaterialDialog { get; } = new();
    public Interaction<string, IStorageFile?> SaveFilePicker { get; } = new();

    #endregion

    #region -- Commands --

    public ReactiveCommand<MaterialLocationViewModel, Unit>
        SelectMaterial { get; }

    public ReactiveCommand<MaterialLocationViewModel, Unit> DeleteMaterial { get; private set; }
    public ReactiveCommand<Unit, Unit> ClearSelection { get; }

    public ReactiveCommand<MaterialLocationViewModel, Unit> CopyMaterial { get; private set; }
    public ReactiveCommand<MaterialLocationViewModel, Unit> PasteMaterial { get; }

    public ReactiveCommand<MaterialLocationViewModel, Unit> LoadMaterial { get; }
    public ReactiveCommand<MaterialLocationViewModel, Unit> Locate { get; private set; }
    public ReactiveCommand<OutputType, Unit> Export { get; }
    public ReactiveCommand<Unit, Unit> Sync { get; }
    public ReactiveCommand<GroupDescriptionViewModel?, Unit> AddGroupDescription { get; }
    public ReactiveCommand<GroupDescriptionViewModel?, Unit> RemoveGroupDescription { get; set; }

    #endregion

    #region -- Constructors --

    internal MaterialsViewModel()
    {
        // Design
    }

    public MaterialsViewModel(NotificationHelper notificationHelper,
        IFunctionLocationStore functionLocationStore,
        IMaterialLocationStore materialLocationStore,
        IMaterialService materialService
    )
    {
#if DEBUG
        DebugExt.Log("Initializing MaterialsViewModel", null, nameof(MaterialsViewModel));
#endif

        // 20250124: use the collection view to support group and sort feature in data grid.
        // The source of the collection view must be a class implements  INotifyCollectionChanged and INotifyPropertyChanged, so that the change of the item can propogate to the collection view.
        // The default sort is provided by sort description, so the SortAndBind method of the DynamicData is no longer used here.
        Locations = new DataGridCollectionView(_locations);
        Locations.SortDescriptions.Add([
            DataGridSortDescription.FromPath(nameof(MaterialLocationViewModel.ProcessArea)),
            DataGridSortDescription.FromPath(nameof(MaterialLocationViewModel.FunctionalGroup)),
            DataGridSortDescription.FromPath(nameof(MaterialLocationViewModel.FunctionalElement))
        ]);

        #region -- Commands --

        Export = ReactiveCommand.CreateFromTask<OutputType, Unit>(async type =>
        {
            switch (type)
            {
                case OutputType.Page:
                    await materialLocationStore.ExportAsEmbeddedObject();
                    break;
                case OutputType.Excel:
                    using (var file = await SaveFilePicker.Handle("xlsx"))
                    {
                        var filePath = file?.TryGetLocalPath();
                        if (filePath is null) return Unit.Default;

                        await materialLocationStore.ExportAsWorkbook(filePath);
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"/select,\"{filePath}\"",
                            UseShellExecute = true
                        });
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            return Unit.Default;
        });
        Export.ThrownExceptions.Subscribe(e => { notificationHelper.Error(e.Message); });

        Sync = ReactiveCommand.CreateFromTask(async _ =>
        {
            var viewModel = new SyncMaterialsViewModel(_locations);
            return await ShowSyncMaterialsDialog.Handle(viewModel);
        });

        AddGroupDescription = ReactiveCommand.Create<GroupDescriptionViewModel?>(groupDescription =>
        {
            if (groupDescription == null || string.IsNullOrEmpty(groupDescription.PropertyName)) return;
            if (GroupDescriptions.Any(x => x.PropertyName == groupDescription.PropertyName)) return;

            GroupDescriptions.Add(groupDescription);
        });
        RemoveGroupDescription = ReactiveCommand.Create<GroupDescriptionViewModel?>(groupDescription =>
        {
            if (groupDescription == null || string.IsNullOrEmpty(groupDescription.PropertyName)) return;
            if (!GroupDescriptions.Contains(groupDescription)) return;

            GroupDescriptions.Remove(groupDescription);
        });

        ClearSelection = ReactiveCommand.Create(() =>
        {
            if (!_locations.Any()) return;
            foreach (var vm in _locations.Where(x => x.IsSelected)) vm.IsSelected = false;
        });

        SelectMaterial =
            ReactiveCommand
                .CreateFromTask<MaterialLocationViewModel>(async location =>
                {
                    var context = new MaterialLocationContext
                    {
                        ProjectId = Project?.Id,
                        FunctionZone = location.ProcessArea,
                        FunctionGroup = location.FunctionalGroup,
                        FunctionElement = location.FunctionalElement,
                        MaterialLocationType = location.MaterialType
                    };

                    var viewModel = new SelectMaterialWindowViewModel(notificationHelper, materialService, context);
                    var dialogResult = await ShowSelectMaterialDialog.Handle(viewModel);
                    if (dialogResult == null) return;

                    if (SelectedLocations.Any())
                        foreach (var selectedLocation in SelectedLocations.ToArray())
                            selectedLocation.MaterialCode = dialogResult!.Code;
                    else
                        location.MaterialCode = dialogResult.Code;
                });

        DeleteMaterial = ReactiveCommand.Create<MaterialLocationViewModel>(location =>
        {
            location.MaterialCode = string.Empty;
        });

        LoadMaterial = ReactiveCommand.CreateFromTask<MaterialLocationViewModel>(async location =>
        {
            var result = await location.GetMaterial();
            Material = new MaterialViewModel(result.Value);
        });
        LoadMaterial.ThrownExceptions
            .Subscribe(e => { notificationHelper.Error("加载物料信息失败", e!.Message); });

        Locate = ReactiveCommand.Create<MaterialLocationViewModel>(location =>
        {
            materialLocationStore.Locate(location.Id);
        });

        var canCopy = this.WhenAnyValue(x => x.SelectedLocation)
            .Select(x => !string.IsNullOrEmpty(x?.MaterialCode));
        CopyMaterial =
            ReactiveCommand.Create<MaterialLocationViewModel>(
                location => Clipboard = (location.MaterialType, location.MaterialCode), canCopy);

        var canPaste = this.WhenAnyValue(x => x.SelectedLocation, x => x.Clipboard,
            (location, clipboard) => location?.MaterialType == clipboard?.Item1);
        PasteMaterial =
            ReactiveCommand.Create<MaterialLocationViewModel>(
                location =>
                {
                    if (Clipboard?.Item1 is { } str && str != location.MaterialType)
                        throw new MaterialTypeNotMatchException(str, location.MaterialType);

                    location.MaterialCode = Clipboard?.Item2 ?? string.Empty;
                }, canPaste);
        PasteMaterial.ThrownExceptions
            .Subscribe(e => { notificationHelper.Error("粘贴物料失败", e!.Message); });

        #endregion

        #region -- Subscriptions --

        var materialLocations = materialLocationStore.MaterialLocations.Connect();

        /* 注意这里必须首先把changeset存成本地变量，否则join之后当发生变化时，会莫名触发removed。*/
        var observeMaterial = materialLocations
#if DEBUG
            .OnItemAdded(x => DebugExt.Log("MaterialLocations.OnItemAdded", x.Location.Id, nameof(MaterialsViewModel)))
            .OnItemUpdated((cur, _, _) =>
                DebugExt.Log("MaterialLocations.OnItemUpdated", cur.Location.Id, nameof(MaterialsViewModel)))
            .OnItemRefreshed(x =>
                DebugExt.Log("MaterialLocations.OnItemRefreshed", x.Location.Id, nameof(MaterialsViewModel)))
            .OnItemRemoved(x =>
                DebugExt.Log("MaterialLocations.OnItemRemoved", x.Location.Id, nameof(MaterialsViewModel)))
#endif
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(_ => { IsLoading = false; });


        var observeFunction = functionLocationStore.FunctionLocations.Connect()
            .Transform(x => x.Location)
#if DEBUG
            .OnItemAdded(x => DebugExt.Log("FunctionLocations.OnItemAdded", x.Id, nameof(MaterialsViewModel)))
            .OnItemUpdated((cur, _, _) =>
                DebugExt.Log("FunctionLocations.OnItemUpdated", cur.Id, nameof(MaterialsViewModel)))
            .OnItemRefreshed(x => DebugExt.Log("FunctionLocations.OnItemRefreshed", x.Id, nameof(MaterialsViewModel)))
            .OnItemRemoved(x => DebugExt.Log("FunctionLocations.OnItemRemoved", x.Id, nameof(MaterialsViewModel)))
#endif
            .ObserveOn(RxApp.MainThreadScheduler);

        var fullTextFilter = this.WhenValueChanged(t => t.SearchText)
            .Throttle(TimeSpan.FromMilliseconds(400))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(BuildFilter);

        observeMaterial
            .InnerJoin<(MaterialLocation Location, Lazy<Task<ResolveResult<Material?>>> Material), ICompoundKey,
                FunctionLocation, ICompoundKey, MaterialLocationViewModel>(
                observeFunction,
                right => right.Id,
                (left, right) => new MaterialLocationViewModel(left.Location, right, left.Material)
            )
            // 20250124: add a side effect to synchronize the IsSelected and IsDisabled property before bind to source
            .Do(x =>
            {
                foreach (var change in x)
                {
                    var item = change.Current;
                    var previous = _locations.SingleOrDefault(i => Equals(i.Id, item.Id));
                    if (previous == null) return;
                    item.IsSelected = previous.IsSelected;
                    item.IsEnabled = previous.IsEnabled;
                }
            })
            .Filter(fullTextFilter)
            .Bind(_locations)
            .Subscribe();

        _locations.ToObservableChangeSet()
            .AutoRefresh(x => x.IsSelected)
            .Filter(x => x.IsSelected)
            .Bind(out _selectedLocations)
            .OnItemAdded(UpdateIsEnabled)
            .OnItemRemoved(ResetIsEnabled)
            .Subscribe();

        this.WhenAnyValue(x => x.Material)
            .WhereNotNull()
            .Subscribe(_ =>
            {
                IsMaterialVisible = false;
                IsMaterialVisible = true;
            });

        // observe the group description change to manage view group descriptions
        GroupDescriptions.ToObservableChangeSet()
            .ObserveOn(RxApp.MainThreadScheduler)
            .OnItemAdded(v => { Locations.GroupDescriptions.Add(new DataGridPathGroupDescription(v.PropertyName)); })
            .OnItemRemoved(v =>
            {
                var description =
                    Locations.GroupDescriptions.SingleOrDefault(x => x.PropertyName == v.PropertyName);
                if (description != null)
                    Locations.GroupDescriptions.Remove(description);
            })
            .Subscribe();

        // observe the viewmodel change and back to service
        var observeChange = _locations.ToObservableChangeSet(t => t.Id);
        observeChange.WhenAnyPropertyChanged(nameof(MaterialLocationViewModel.Quantity),
                nameof(MaterialLocationViewModel.MaterialCode))
            .WhereNotNull()
            .Select(x => x.MaterialSource with { Quantity = x.Quantity, Code = x.MaterialCode })
            .Subscribe(x => materialLocationStore.Update([x]));

        observeChange.WhenAnyPropertyChanged(nameof(MaterialLocationViewModel.Description),
                nameof(MaterialLocationViewModel.Remarks))
            .WhereNotNull()
            .Select(x => x.FunctionSource! with { Description = x.Description, Remarks = x.Remarks })
            .Subscribe(x => functionLocationStore.Update([x]));

        #endregion

        return;

        Func<MaterialLocationViewModel, bool> BuildFilter(string? searchText)
        {
            if (string.IsNullOrEmpty(searchText)) return _ => true;

            return material => material.Contains(searchText!);
        }
    }

    /// <summary>
    ///     The project is used for deciding whether the material synchronization could be chosen.
    /// </summary>
    public ProjectViewModel? Project
    {
        get => _project;
        set => this.RaiseAndSetIfChanged(ref _project, value);
    }

    #endregion
}