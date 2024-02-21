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
    private ReadOnlyObservableCollection<LineItemBase> _items;
    private DocumentInfoViewModel _documentInfo;

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

    public ReadOnlyObservableCollection<LineItemBase> Items => _items;

    #endregion

    protected override void SetupCommands()
    {
        Submit = ReactiveCommand.Create(ExportAsBOMTable);
        Cancel = ReactiveCommand.Create(() => { });
    }

    protected override void SetupSubscriptions(CompositeDisposable d)
    {
        new DocumentExporter(Globals.ThisAddIn.Application.ActivePage)
            .Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _items)
            .Subscribe().DisposeWith(d);
    }

    protected override void SetupStart()
    {
        _documentInfo = new DocumentInfoViewModel();
        _documentInfo.Load();
    }

    protected override void SetupDeactivate()
    {
        _documentInfo.Cache();
    }

    private void ExportAsBOMTable()
    {
        DocumentExporter.SaveAsBom(_items, 
            _documentInfo.CustomerName, 
            _documentInfo.DocumentNo,
            _documentInfo.ProjectNo,
            _documentInfo.VersionNo);
    }
}