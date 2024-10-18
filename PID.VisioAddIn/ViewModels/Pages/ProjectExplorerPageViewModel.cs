using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.EventArgs;
using AE.PID.Visio.Core;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using ReactiveUI;
using Splat;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace AE.PID.ViewModels;

public class ProjectExplorerPageViewModel(IProjectService? service = null)
    : ViewModelBase, IEnableLogger
{
    private readonly IProjectService _service = service ?? Locator.Current.GetService<IProjectService>()!;
    private MaterialLocationViewModel? _copySource;
    private ObservableAsPropertyHelper<bool> _isLoading = ObservableAsPropertyHelper<bool>.Default(true);
    private MaterialLocationViewModel? _selected;

    #region Output Properties

    public ObservableCollection<MaterialTreeViewModel> StructureMaterials { get; set; } = new([]);

    public ObservableCollection<FlattenMaterialLocationViewModel> FlattenMaterials { get; } = new([]);

    public bool IsLoading => _isLoading.Value;

    #endregion

    #region Read-Only Properties

    public OkCancelFeedbackViewModel OkCancelFeedbackViewModel { get; } = new();
    public ReactiveCommand<Unit, Unit>? CopyMaterial { get; private set; }
    public ReactiveCommand<Unit, Unit>? PasteMaterial { get; private set; }
    public ReactiveCommand<Unit, Unit>? ExportToPage { get; private set; }

    #endregion

    #region Setups

    protected override void SetupCommands()
    {
        OkCancelFeedbackViewModel.Ok = ReactiveCommand.Create(() =>
        {
            var dialog = new SaveFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Filter = @"Excel Files|*.xlsx|All Files|*.*""",
                Title = @"保存文件"
            };
            if (dialog.ShowDialog() is true)
                _service.ExportToExcel(dialog.FileName);
        });
        OkCancelFeedbackViewModel.Cancel = ReactiveCommand.Create(() => { });

        ExportToPage = ReactiveCommand.CreateRunInBackground(() => _service.ExportToPage(),
            backgroundScheduler: ThisAddIn.Scheduler);

        // copy design material is allowed if the selected item has material no
        var canCopy = this.WhenAnyValue(x => x.Selected,
            x => x != null && !string.IsNullOrEmpty(x.Code));
        CopyMaterial = ReactiveCommand.Create(() => { CopySource = Selected; }, canCopy);

        // paste material is allowed if the item is of the same type, that is only copy equipment to equipment, instrument to instrument
        var canPaste = this.WhenAnyValue(x => x.CopySource, x => x.Selected,
            (source, target) => source != null && target != null && source.GetType() == target.GetType());
        PasteMaterial = ReactiveCommand.Create(() =>
        {
            if (Selected != null && CopySource != null)
                Selected.Code = CopySource.Code;
        }, canPaste);
    }

    protected override void SetupSubscriptions(CompositeDisposable d)
    {
        var functionChangeSet = _service.FunctionLocations.Connect();
        var materialChangeSet = _service.MaterialLocations.Connect();

        functionChangeSet
            .AutoRefresh(t => t.ParentId)
            .LeftJoin(materialChangeSet, x => x.LocationId,
                (function, optionalMaterial) => optionalMaterial.HasValue
                    ? new StructureMaterialLocationViewModel(function, optionalMaterial.Value)
                    : new StructureMaterialLocationViewModel(function))
            .TransformToTree(x => x.ParentId,
                Observable.Return((Func<Node<StructureMaterialLocationViewModel, CompositeId>, bool>)DefaultPredicate))
            .Transform(x => new MaterialTreeViewModel(x))
            .ObserveOn(App.UIScheduler)
            .SortAndBind(StructureMaterials, SortExpressionComparer<MaterialTreeViewModel>.Ascending(t => t.Name))
            .DisposeMany()
            .Subscribe()
            .DisposeWith(d);

        materialChangeSet
            .LeftJoin(functionChangeSet, x => x.Id,
                (material, optionalFunction) => optionalFunction.HasValue
                    ? new FlattenMaterialLocationViewModel(material, optionalFunction.Value)
                    : new FlattenMaterialLocationViewModel(material))
            .ObserveOn(App.UIScheduler)
            .SortAndBind(FlattenMaterials,
                SortExpressionComparer<FlattenMaterialLocationViewModel>
                    .Ascending(t => t.ProcessArea)
                    .ThenByAscending(t => t.FunctionalGroup)
                    .ThenByAscending(t => t.FunctionalElement))
            .Subscribe()
            .DisposeWith(d);

        StructureMaterials.ToObservableChangeSet()
            .IsEmpty()
            .ObserveOn(App.UIScheduler)
            .ToProperty(this, x => x.IsLoading, out _isLoading)
            .DisposeWith(d);

        // whenever there is a selected element, notify the View to show the side page
        var selectedItem = this.WhenAnyValue(x => x.Selected)
            .DistinctUntilChanged()
            .ObserveOn(App.UIScheduler);
        selectedItem.Subscribe()
            .DisposeWith(d);

        // highlight the item on the page
        selectedItem.WhereNotNull()
            .ObserveOn(ThisAddIn.Scheduler)
            .Subscribe(x => service.Select(x.ShapeId))
            .DisposeWith(d);

        // notify the side view model for seeding
        MessageBus.Current.RegisterMessageSource(selectedItem.WhereNotNull().Select(x =>
                new MaterialLocationSelectedEventArgs(x)))
            .DisposeWith(d);
        return;

        bool DefaultPredicate(Node<StructureMaterialLocationViewModel, CompositeId> node)
        {
            return node.IsRoot;
        }
    }

    #endregion

    #region Read-Write Properties

    public MaterialLocationViewModel? Selected
    {
        get => _selected;
        set => this.RaiseAndSetIfChanged(ref _selected, value);
    }

    public MaterialLocationViewModel? CopySource
    {
        get => _copySource;
        set => this.RaiseAndSetIfChanged(ref _copySource, value);
    }

    #endregion
}