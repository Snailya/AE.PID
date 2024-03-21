using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Controllers.Services;
using AE.PID.Models.BOM;
using DynamicData;
using ReactiveUI;

namespace AE.PID.ViewModels.Pages;

public class ExportViewModel(DocumentExporter service) : ViewModelBase
{
    private DocumentInfoViewModel _documentInfo;
    private ReadOnlyObservableCollection<ElementViewModel> _items;
    private ElementViewModel? _selected;

    #region Read-Write Properties

    public DocumentInfoViewModel DocumentInfo
    {
        get => _documentInfo;
        private set => this.RaiseAndSetIfChanged(ref _documentInfo, value);
    }

    public ElementViewModel? Selected
    {
        get => _selected;
        set => this.RaiseAndSetIfChanged(ref _selected, value);
    }

    #endregion

    #region Read-Only Properties

    public DesignMaterialsViewModel DesignMaterialsViewModel { get; private set; } =
        new(Globals.ThisAddIn.ServiceManager.MaterialsService);

    public OkCancelFeedbackViewModel OkCancelFeedbackViewModel { get; private set; } = new();

    #endregion

    #region Output Properties

    public ReadOnlyObservableCollection<ElementViewModel> Items => _items;

    #endregion

    protected override void SetupCommands()
    {
        DesignMaterialsViewModel.Select = ReactiveCommand.Create<DesignMaterial>(WriteDesignMaterialIntoShape);
        DesignMaterialsViewModel.Close = ReactiveCommand.Create(() => { });

        OkCancelFeedbackViewModel.Ok = ReactiveCommand.Create(ExportAsBOMTable);
        OkCancelFeedbackViewModel.Cancel = ReactiveCommand.Create(() => { });
    }
    
    protected override void SetupSubscriptions(CompositeDisposable d)
    {
        service.Elements
            .Connect()
            .TransformToTree(x => x.ParentId, Observable.Return(DefaultPredicate))
            .Transform(node => new ElementViewModel(node))
            .Sort(new ElementViewModelComparer())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _items)
            .DisposeMany()
            .Subscribe()
            .DisposeWith(d);

        service.MonitorChange()
            .DisposeWith(d);

        this.WhenAnyValue(x => x.Selected)
            .WhereNotNull()
            .Select(x => _items.SingleOrDefault(i => i.Id == x.Id))
            .WhereNotNull()
            .Select(x => x.Name)
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x => { DesignMaterialsViewModel.ElementName = x; })
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

    private void WriteDesignMaterialIntoShape(DesignMaterial? material)
    {
        if (material == null || _selected?.Id == null) return;
            
        var id = _selected.Id;
        service.SetDesignMaterial(id, material);
    }
    
    private void ExportAsBOMTable()
    {
        service.ExportToExcel(_documentInfo);
    }
}