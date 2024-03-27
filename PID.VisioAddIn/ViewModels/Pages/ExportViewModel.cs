using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Controllers.Services;
using AE.PID.Models.BOM;
using AE.PID.Models.EventArgs;
using DynamicData;
using ReactiveUI;

namespace AE.PID.ViewModels.Pages;

public class ExportViewModel(DocumentExporter service) : ViewModelBase
{
    private DocumentInfoViewModel _documentInfo;
    private ReadOnlyObservableCollection<ElementViewModel> _items;
    private ElementViewModel? _selected;

    #region Output Properties

    public ReadOnlyObservableCollection<ElementViewModel> Items => _items;

    #endregion

    #region Read-Only Properties

    public OkCancelFeedbackViewModel OkCancelFeedbackViewModel { get; } = new();

    #endregion

    protected override void SetupCommands()
    {
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

        // whenever there is a selected element, notify the View to show the side page
        var selectedItem = this.WhenAnyValue(x => x.Selected)
            .WhereNotNull()
            .Select(x => _items.SingleOrDefault(i => i.Id == x.Id))
            .WhereNotNull()
            .Select(x => x.Name)
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler);
        selectedItem.Subscribe()
            .DisposeWith(d);

        // notify the side view model for seeding
        MessageBus.Current.RegisterMessageSource(selectedItem.Select(x => new ElementSelectedEventArgs(x)));

        // when user select the material from selection page, write this into element
        MessageBus.Current.Listen<DesignMaterialSelectedEventArgs>()
            .Subscribe(x => WriteDesignMaterialIntoElement(x.DesignMaterial))
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

    private void WriteDesignMaterialIntoElement(DesignMaterial? material)
    {
        if (material == null || _selected?.Id == null) return;

        var id = _selected.Id;
        service.SetDesignMaterial(id, material);
    }

    private void ExportAsBOMTable()
    {
        service.ExportToExcel(_documentInfo);
    }

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
}