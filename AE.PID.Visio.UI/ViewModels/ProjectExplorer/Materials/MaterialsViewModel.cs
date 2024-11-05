using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using AE.PID.Core.Models;
using AE.PID.Visio.Core.Exceptions;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using AE.PID.Visio.Shared.Extensions;
using AE.PID.Visio.UI.Avalonia.Models;
using AE.PID.Visio.UI.Avalonia.Services;
using Avalonia.Platform.Storage;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class MaterialsViewModel : ViewModelBase
{
    private readonly ReadOnlyObservableCollection<MaterialLocationViewModel> _locations;
    private readonly ReadOnlyObservableCollection<MaterialLocationViewModel> _selectedLocations;

    private ValueTuple<string, string>? _clipboard;
    private bool _isLoading = true;
    private bool _isMaterialVisible;
    private MaterialViewModel? _material;
    private MaterialLocationViewModel? _selectedLocation;

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
    public ReadOnlyObservableCollection<MaterialLocationViewModel> Locations => _locations;

    public bool IsMaterialVisible
    {
        get => _isMaterialVisible;
        set => this.RaiseAndSetIfChanged(ref _isMaterialVisible, value);
    }

    private void ResetIsEnabled(MaterialLocationViewModel x)
    {
        if (SelectedLocations.Any()) return;
        foreach (var vm in Locations)
            vm.IsEnabled = true;
    }

    private void UpdateIsEnabled(MaterialLocationViewModel x)
    {
        if (SelectedLocations.Count != 1) return;
        foreach (var vm in Locations)
            vm.IsEnabled = x.MaterialType == vm.MaterialType;
    }


    #region -- Interactions --

    public Interaction<SyncMaterialsViewModel, Unit> ShowSyncMaterialsDialog { get; } = new();
    public Interaction<SelectMaterialWindowViewModel?, MaterialViewModel?> ShowSelectMaterialDialog { get; } = new();
    public Interaction<Unit, IStorageFile?> SaveFilePicker { get; } = new();

    #endregion

    #region -- Commands --

    public ReactiveCommand<MaterialLocationViewModel, Unit> SelectMaterial { get; private set; }
    public ReactiveCommand<MaterialLocationViewModel, Unit> DeleteMaterial { get; private set; }
    public ReactiveCommand<Unit, Unit> ClearSelection { get; }

    public ReactiveCommand<MaterialLocationViewModel, Unit> CopyMaterial { get; private set; }
    public ReactiveCommand<MaterialLocationViewModel, Unit> PasteMaterial { get; }

    public ReactiveCommand<MaterialLocationViewModel, Unit> LoadMaterial { get; }
    public ReactiveCommand<MaterialLocationViewModel, Unit> Locate { get; private set; }
    public ReactiveCommand<OutputType, Unit> Export { get; }
    public ReactiveCommand<Unit, Unit> Sync { get; }

    #endregion

    #region -- Constructors --

    internal MaterialsViewModel()
    {
        // Design
    }

    public MaterialsViewModel(NotificationHelper notificationHelper,
        IProjectStore projectStore,
        IFunctionLocationStore functionLocationStore,
        IMaterialLocationStore materialLocationStore,
        IMaterialService materialService)
    {
#if DEBUG
        DebugExt.Log("Initializing MaterialsViewModel",null, nameof(MaterialsViewModel));
#endif

        #region -- Commands --

        Export = ReactiveCommand.CreateFromTask<OutputType, Unit>(async type =>
        {
            switch (type)
            {
                case OutputType.Page:
                    await materialLocationStore.ExportAsEmbeddedObject();
                    break;
                case OutputType.Excel:
                    using (var file = await SaveFilePicker.Handle(Unit.Default))
                    {
                        var filePath = file?.TryGetLocalPath();
                        if (filePath is null) return Unit.Default;

                        await materialLocationStore.ExportAsWorkbook(filePath!);
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
            var viewModel = new SyncMaterialsViewModel(Locations);
            return await ShowSyncMaterialsDialog.Handle(viewModel);
        });

        ClearSelection = ReactiveCommand.Create(() =>
        {
            foreach (var vm in Locations.Where(x => x.IsSelected)) vm.IsSelected = false;
        });

        SelectMaterial = ReactiveCommand.CreateFromTask<MaterialLocationViewModel>(async location =>
        {
            var context = new MaterialLocationContext
            {
                ProjectId = projectStore.GetCurrentProject()?.Id,
                FunctionZone = location.ProcessArea,
                FunctionGroup = location.FunctionalGroup,
                FunctionElement = location.FunctionalElement,
                MaterialLocationType = location.MaterialType
            };

            var viewModel = new SelectMaterialWindowViewModel(notificationHelper, materialService, context);
            var dialogResult = await ShowSelectMaterialDialog.Handle(viewModel);
            if (dialogResult == null) return;

            if (SelectedLocations.Any())
                foreach (var selectedLocation in SelectedLocations)
                    selectedLocation.SetMaterial(dialogResult);
            else
                location.SetMaterial(dialogResult);
        });

        DeleteMaterial = ReactiveCommand.Create<MaterialLocationViewModel>(location =>
        {
            location.MaterialCode = string.Empty;
        });

        LoadMaterial = ReactiveCommand.CreateFromTask<MaterialLocationViewModel>(async location =>
        {
            var result = await materialService.GetByCodeAsync(location.MaterialCode);
            Material = new MaterialViewModel(result);
        });
        LoadMaterial.ThrownExceptions
            .Subscribe(v => { notificationHelper.Error("加载物料信息失败", v!.Message); });

        Locate = ReactiveCommand.Create<MaterialLocationViewModel>(location =>
        {
            materialLocationStore.Locate(location.FunctionId);
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
            .Subscribe(v => { notificationHelper.Error("粘贴物料失败", v!.Message); });

        #endregion

        #region -- Subscriptions --

        /* 注意这里必须首先把changeset存成本地变量，否则join之后当发生变化时，会莫名触发removed。*/
        var observeMaterial = materialLocationStore.MaterialLocations.Connect()
            .AutoRefresh()
#if DEBUG
            .OnItemAdded(x => DebugExt.Log("MaterialLocations.OnItemAdded", x.LocationId, nameof(MaterialsViewModel)))
            .OnItemUpdated((cur, prev, _) =>
                DebugExt.Log("MaterialLocations.OnItemUpdated", cur.LocationId, nameof(MaterialsViewModel)))
            .OnItemRefreshed(x => DebugExt.Log("MaterialLocations.OnItemRefreshed", x.LocationId, nameof(MaterialsViewModel)))
            .OnItemRemoved(x => DebugExt.Log("MaterialLocations.OnItemRemoved", x.LocationId, nameof(MaterialsViewModel)))
#endif
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(x => { IsLoading = false; });

        var observeFunction = functionLocationStore.FunctionLocations.Connect()
            .AutoRefresh()
#if DEBUG
            .OnItemAdded(x => DebugExt.Log("FunctionLocations.OnItemAdded", x.Id, nameof(MaterialsViewModel)))
            .OnItemUpdated((cur, prev, _) =>
                DebugExt.Log("FunctionLocations.OnItemUpdated", cur.Id, nameof(MaterialsViewModel)))
            .OnItemRefreshed(x => DebugExt.Log("FunctionLocations.OnItemRefreshed", x.Id, nameof(MaterialsViewModel)))
            .OnItemRemoved(x => DebugExt.Log("FunctionLocations.OnItemRemoved", x.Id, nameof(MaterialsViewModel)))
#endif
            .ObserveOn(RxApp.MainThreadScheduler);

        observeMaterial
            .InnerJoin<MaterialLocation, CompositeId, FunctionLocation, CompositeId, MaterialLocationViewModel>(
                observeFunction,
                right => right.Id,
                (left, right) => new MaterialLocationViewModel(left, right)
            )
            .SortAndBind(out _locations,
                SortExpressionComparer<MaterialLocationViewModel>.Ascending(x => x.ProcessArea)
                    .ThenByAscending(x => x.FunctionalGroup)
                    .ThenByAscending(x => x.FunctionalElement)
            )
            .Subscribe();

        Locations.ToObservableChangeSet()
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

        #endregion
    }

    #endregion
}