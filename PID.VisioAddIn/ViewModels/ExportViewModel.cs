using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AE.PID.Controllers.Services;
using AE.PID.Models;
using AE.PID.Models.BOM;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class ExportViewModel : ViewModelBase
{
    private readonly ObservableCollectionExtended<LineItemBase> _itemsSource = [];
    private ReadOnlyObservableCollection<LineItemBase> _items;
    private string _customerName;
    private string _documentNo;
    private string _projectNo;
    private string _versionNo;

    #region Read-Write Properties

    public string CustomerName
    {
        get => _customerName;
        private set => this.RaiseAndSetIfChanged(ref _customerName, value);
    }

    public string DocumentNo
    {
        get => _documentNo;
        private set => this.RaiseAndSetIfChanged(ref _documentNo, value);
    }

    public string ProjectNo
    {
        get => _projectNo;
        private set => this.RaiseAndSetIfChanged(ref _projectNo, value);
    }

    public string VersionNo
    {
        get => _versionNo;
        private set => this.RaiseAndSetIfChanged(ref _versionNo, value);
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
        _itemsSource.ToObservableChangeSet()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _items)
            .Subscribe()
            .DisposeWith(d);
    }

    protected override void SetupStart()
    {
        LoadInputCache();
        LoadItemsAsync();
    }

    private Task LoadItemsAsync()
    {
        return Task.Run(() => _itemsSource.AddRange(DocumentExporter.GetLineItems()));
    }

    protected override void SetupDeactivate()
    {
        CacheUserInputs();
    }

    private void LoadInputCache()
    {
        CustomerName = Globals.ThisAddIn.InputCache.CustomerName;
        DocumentNo = Globals.ThisAddIn.InputCache.DocumentNo;
        ProjectNo = Globals.ThisAddIn.InputCache.ProjectNo;
        VersionNo = Globals.ThisAddIn.InputCache.VersionNo;
    }

    private void CacheUserInputs()
    {
        Globals.ThisAddIn.InputCache.CustomerName = _customerName;
        Globals.ThisAddIn.InputCache.DocumentNo = _documentNo;
        Globals.ThisAddIn.InputCache.ProjectNo = _projectNo;
        Globals.ThisAddIn.InputCache.VersionNo = _versionNo;
        InputCache.Save(Globals.ThisAddIn.InputCache);
    }

    private void ExportAsBOMTable()
    {
        DocumentExporter.SaveAsBom(_itemsSource, _customerName, _documentNo, _projectNo,
            _versionNo);
    }
}