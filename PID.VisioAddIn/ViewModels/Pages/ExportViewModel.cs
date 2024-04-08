using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Controllers.Services;
using AE.PID.Models.BOM;
using AE.PID.Models.EventArgs;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.ViewModels.Pages;

public class ExportViewModel(DocumentExporter service) : ViewModelBase
{
    private ReadOnlyObservableCollection<TreeNodeViewModel<Element>> _bomTree = new([]);
    private DocumentInfoViewModel _documentInfo;
    private Element? _selected;


    #region Output Properties

    public ReadOnlyObservableCollection<TreeNodeViewModel<Element>> BOMTree => _bomTree;

    #endregion

    #region Read-Only Properties

    public OkCancelFeedbackViewModel OkCancelFeedbackViewModel { get; } = new();

    #endregion

    #region Command Handlers

    private void ExportToExcel()
    {
        service.ExportToExcel(_documentInfo);
    }

    #endregion

    #region Setups

    protected override void SetupCommands()
    {
        OkCancelFeedbackViewModel.Ok = ReactiveCommand.Create(ExportToExcel);
        OkCancelFeedbackViewModel.Cancel = ReactiveCommand.Create(() => { });
    }

    protected override void SetupSubscriptions(CompositeDisposable d)
    {
        service.Elements
            .ToObservableChangeSet(t => t.Id)
            .AutoRefresh(t => t.ParentId)
            .TransformToTree(x => x.ParentId)
            .Transform(x => new TreeNodeViewModel<Element>(x))
            .Bind(out _bomTree)
            .DisposeMany()
            .Subscribe()
            .DisposeWith(d);
        
        // whenever there is a selected element, notify the View to show the side page
        var selectedItem = this.WhenAnyValue(x => x.Selected)
            .WhereNotNull()
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler);
        selectedItem.Subscribe()
            .DisposeWith(d);

        // notify the side view model for seeding
        MessageBus.Current.RegisterMessageSource(selectedItem.Select(x =>
            new ElementSelectedEventArgs(x)));
    }

    protected override void SetupStart()
    {
        _documentInfo = new DocumentInfoViewModel(Globals.ThisAddIn.Application.ActivePage);
    }

    #endregion


    #region Read-Write Properties

    public DocumentInfoViewModel DocumentInfo
    {
        get => _documentInfo;
        private set => this.RaiseAndSetIfChanged(ref _documentInfo, value);
    }

    public Element? Selected
    {
        get => _selected;
        set => this.RaiseAndSetIfChanged(ref _selected, value);
    }

    #endregion
}