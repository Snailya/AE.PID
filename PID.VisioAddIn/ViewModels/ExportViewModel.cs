using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Controllers.Services;
using AE.PID.Models.BOM;
using DynamicData;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class ExportViewModel : ViewModelBase
{
    private readonly DocumentExporter _service;

    private DocumentInfoViewModel _documentInfo;
    private ReadOnlyObservableCollection<ElementViewModel> _items;
    private ElementViewModel _selected;

    public ExportViewModel(DocumentExporter service)
    {
        _service = service;
        DesignMaterialsViewModel = new DesignMaterialsControlViewModel(new DesignMaterialService());
    }

    #region Read-Write Properties

    public DocumentInfoViewModel DocumentInfo
    {
        get => _documentInfo;
        private set => this.RaiseAndSetIfChanged(ref _documentInfo, value);
    }

    public ElementViewModel Selected
    {
        get => _selected;
        set => this.RaiseAndSetIfChanged(ref _selected, value);
    }

    #endregion

    #region Read-Only Properties

    public DesignMaterialsControlViewModel DesignMaterialsViewModel { get; private set; }
    public ReactiveCommand<Unit, Unit> Submit { get; private set; }
    public ReactiveCommand<Unit, Unit> Cancel { get; private set; }

    #endregion

    #region Output Properties

    public ReadOnlyObservableCollection<ElementViewModel> Items => _items;

    #endregion

    protected override void SetupCommands()
    {
        Submit = ReactiveCommand.Create(ExportAsBOMTable);
        Cancel = ReactiveCommand.Create(() => { });
        DesignMaterialsViewModel.Select = ReactiveCommand.Create(() =>
        {
            var designMaterial = DesignMaterialsViewModel.Selected;
            if (designMaterial == null) return;

            var id = _selected.Id;
            _service.SetDesignMaterial(id, designMaterial.Id);
        });
    }

    protected override void SetupSubscriptions(CompositeDisposable d)
    {
        _service.Elements
            .Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .TransformToTree(x => x.ParentId, Observable.Return(DefaultPredicate))
            .Transform(node => new ElementViewModel(node))
            .Sort(new ElementViewModelComparer())
            .Bind(out _items)
            .DisposeMany()
            .Subscribe()
            .DisposeWith(d);

        _service.MonitorChange()
            .DisposeWith(d);

        this.WhenAnyValue(x => x.Selected)
            .Where(x => x != null)
            .Select(x => x.Id)
            .DistinctUntilChanged()
            .Subscribe(x => { DesignMaterialsViewModel.Seed = _items.SingleOrDefault(i => i.Id == x); })
            .DisposeWith(d);

        return;

        bool DefaultPredicate(Node<Element, int> node)
        {
            return node.IsRoot;
        }
    }

    protected override void SetupStart()
    {
        _documentInfo = new DocumentInfoViewModel(Globals.ThisAddIn.Application.ActivePage);
    }

    protected override void SetupDeactivate()
    {
    }

    private void ExportAsBOMTable()
    {
        // todo:
        _service.ExportToExcel(_documentInfo);
    }
}