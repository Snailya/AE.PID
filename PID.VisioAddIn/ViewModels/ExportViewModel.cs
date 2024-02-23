using System;
using System.Collections.ObjectModel;
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
    private ReadOnlyObservableCollection<ElementViewModel> _items;
    private DocumentInfoViewModel _documentInfo;
    private readonly DocumentExporter _service;

    public ExportViewModel(DocumentExporter service)
    {
        _service = service;
    }

    #region Read-Write Properties

    public DocumentInfoViewModel DocumentInfo
    {
        get => _documentInfo;
        private set => this.RaiseAndSetIfChanged(ref _documentInfo, value);
    }

    #endregion

    #region Read-Only Properties

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
    }

    protected override void SetupSubscriptions(CompositeDisposable d)
    {
        var subscription = _service
            .Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .TransformToTree(x => x.ParentId, Observable.Return(DefaultPredicate))
            .Transform(node => new ElementViewModel(node))
            .Sort(new ElementViewModelComparer())
            .Bind(out _items)
            .DisposeMany()
            .Subscribe();

        CleanUp = Disposable.Create(() => subscription.Dispose());
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