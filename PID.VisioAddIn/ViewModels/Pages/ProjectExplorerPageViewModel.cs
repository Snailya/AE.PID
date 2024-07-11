using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.EventArgs;
using AE.PID.Models;
using AE.PID.Services;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using TaskStatus = AE.PID.Core.Models.TaskStatus;

namespace AE.PID.ViewModels;

public class ProjectExplorerPageViewModel(ProjectService service) : ViewModelBase
{
    private PartItem? _copySource;
    private ReadOnlyObservableCollection<TreeNodeViewModel<ElementBase>> _elementTree = new([]);
    private ObservableAsPropertyHelper<bool> _isElementsLoading = ObservableAsPropertyHelper<bool>.Default();
    private ReadOnlyObservableCollection<PartItem> _partListItems = new([]);
    private ElementBase? _selected;

    #region Output Properties

    public ReadOnlyObservableCollection<TreeNodeViewModel<ElementBase>> ElementTree => _elementTree;
    public ReadOnlyObservableCollection<PartItem> PartListItems => _partListItems;

    public bool IsElementsLoading => _isElementsLoading.Value;

    #endregion

    #region Read-Only Properties

    public DocumentInfoViewModel DocumentInfo { get; } = new();
    public OkCancelFeedbackViewModel OkCancelFeedbackViewModel { get; } = new();
    public ReactiveCommand<Unit, Unit>? CopyMaterial { get; private set; }
    public ReactiveCommand<Unit, Unit>? PasteMaterial { get; private set; }

    #endregion

    #region Setups

    protected override void SetupCommands()
    {
        OkCancelFeedbackViewModel.Ok = ReactiveCommand.Create(() => service.ExportToExcel(DocumentInfo));
        OkCancelFeedbackViewModel.Cancel = ReactiveCommand.Create(() => { });

        // copy design material is allowed if the selected item has material no
        var canCopy = this.WhenAnyValue(x => x.Selected,
            x => x is PartItem partItem && !string.IsNullOrEmpty(partItem.MaterialNo));
        CopyMaterial = ReactiveCommand.Create(() => { CopySource = (PartItem)Selected!; }, canCopy);

        // paste material is allowed if the item is of the same type, that is only copy equipment to equipment, instrument to instrument
        var canPaste = this.WhenAnyValue(x => x.CopySource, x => x.Selected,
            (source, target) => source != null && target != null && source.GetType() == target.GetType());
        PasteMaterial = ReactiveCommand.Create(() =>
        {
            if (Selected is PartItem partItem && CopySource != null)
                partItem.CopyMaterialFrom(CopySource);
        }, canPaste);
    }

    protected override void SetupSubscriptions(CompositeDisposable d)
    {
        Observable.Create<TaskStatus>(observer =>
            {
                observer.OnNext(TaskStatus.Created);

                try
                {
                    observer.OnNext(TaskStatus.Running);
                    service.LoadElements();
                }
                catch (Exception e)
                {
                    observer.OnError(e);
                }

                observer.OnNext(TaskStatus.RanToCompletion);
                observer.OnCompleted();

                return () => { };
            })
            .SubscribeOn(ThisAddIn.Dispatcher!)
            .ObserveOn(WindowManager.Dispatcher!)
            .Select(x => x == TaskStatus.Running)
            .ToProperty(this, x => x.IsElementsLoading, out _isElementsLoading)
            .DisposeWith(d);

        service.Elements
            .Connect()
            .AutoRefresh(t => t.ParentId)
            .TransformToTree(x => x.ParentId)
            .Transform(x => new TreeNodeViewModel<ElementBase>(x))
            .Sort(SortExpressionComparer<TreeNodeViewModel<ElementBase>>.Ascending(t => t.Source!.Label))
            .ObserveOn(WindowManager.Dispatcher!)
            .Bind(out _elementTree)
            .DisposeMany()
            .Subscribe()
            .DisposeWith(d);

        service.Elements
            .Connect()
            .Filter(x => x is PartItem)
            .Transform(x => (PartItem)x)
            .ObserveOn(WindowManager.Dispatcher!)
            .Bind(out _partListItems)
            .DisposeMany()
            .Subscribe()
            .DisposeWith(d);

        // whenever there is a selected element, notify the View to show the side page
        var selectedItem = this.WhenAnyValue(x => x.Selected)
            .WhereNotNull()
            .DistinctUntilChanged()
            .ObserveOn(WindowManager.Dispatcher!);
        selectedItem.Subscribe()
            .DisposeWith(d);

        // notify the side view model for seeding
        MessageBus.Current.RegisterMessageSource(selectedItem.Select(x =>
                new ElementSelectedEventArgs(x)))
            .DisposeWith(d);
    }

    protected override void SetupStart()
    {
        ThisAddIn.Dispatcher!.InvokeAsync(service.Start);
    }

    protected override void SetupDeactivate()
    {
        ThisAddIn.Dispatcher!.InvokeAsync(service.Stop);
    }

    #endregion

    #region Read-Write Properties

    public ElementBase? Selected
    {
        get => _selected;
        set => this.RaiseAndSetIfChanged(ref _selected, value);
    }

    public PartItem? CopySource
    {
        get => _copySource;
        set => this.RaiseAndSetIfChanged(ref _copySource, value);
    }

    #endregion
}