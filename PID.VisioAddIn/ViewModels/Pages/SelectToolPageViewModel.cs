using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using AE.PID.Services;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class SelectToolPageViewModel(SelectService service) : ViewModelBase
{
    private bool _hasSelection;
    private ObservableAsPropertyHelper<bool> _isMastersLoading = ObservableAsPropertyHelper<bool>.Default();
    private ReadOnlyObservableCollection<MasterOptionViewModel> _masters = new([]);
    private SelectionMode _mode = SelectionMode.ById;
    private int _shapeId;

    #region Output Properties

    public ReadOnlyObservableCollection<MasterOptionViewModel> Masters => _masters;
    public bool IsMastersLoading => _isMastersLoading.Value;

    #endregion

    #region Setups

    protected override void SetupCommands()
    {
        var canSelect = this.WhenAnyValue(
            x => x.Mode,
            x => x.ShapeId,
            x => x.HasSelection,
            (type, shapeId, hasSelection) =>
            {
                if (type == SelectionMode.ById) return shapeId > 0;
                return hasSelection;
            });

        OkCancelFeedbackViewModel.Ok = ReactiveCommand.Create(() =>
            {
                var dispatcherOperation = ThisAddIn.Dispatcher!.InvokeAsync(() =>
                {
                    return Mode == SelectionMode.ById ? service.SelectShapeById(_shapeId) : SelectService.SelectShapesByMasters(_masters.Where(x => x.IsChecked).Select(x => x.BaseId));
                });

                if (dispatcherOperation.Result == false) WindowManager.ShowDialog("没有找到", MessageBoxButton.OK);
            },
            canSelect);

        OkCancelFeedbackViewModel.Cancel = ReactiveCommand.Create(() => { });
    }

    protected override void SetupSubscriptions(CompositeDisposable d)
    {
        Observable.Create<TaskStatus>(observer =>
            {
                observer.OnNext(TaskStatus.Created);

                try
                {
                    observer.OnNext(TaskStatus.Running);
                    service.LoadMasters();
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
            .Select(x => x == TaskStatus.Running)
            .ObserveOn(WindowManager.Dispatcher!)
            .ToProperty(this, x => x.IsMastersLoading, out _isMastersLoading)
            .DisposeWith(d);

        service.Masters
            .Connect()
            .Transform(x => new MasterOptionViewModel(x))
            .Sort(SortExpressionComparer<MasterOptionViewModel>.Ascending(t => t.Name))
            .ObserveOn(WindowManager.Dispatcher!)
            .Bind(out _masters)
            .DisposeMany()
            .Subscribe()
            .DisposeWith(d);

        Masters.ToObservableChangeSet()
            .FilterOnObservable(static item =>
                item.WhenPropertyChanged(x => x.IsChecked)
                    .Select(x => x.Value)
            )
            .IsNotEmpty()
            .BindTo(this, x => x.HasSelection)
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

    public SelectionMode Mode
    {
        get => _mode;
        set => this.RaiseAndSetIfChanged(ref _mode, value);
    }

    public int ShapeId
    {
        get => _shapeId;
        set => this.RaiseAndSetIfChanged(ref _shapeId, value);
    }

    #endregion

    #region Read-Only Properties

    public OkCancelFeedbackViewModel OkCancelFeedbackViewModel { get; } = new();

    public bool HasSelection
    {
        get => _hasSelection;
        private set => this.RaiseAndSetIfChanged(ref _hasSelection, value);
    }

    #endregion
}

public enum SelectionMode
{
    ById,
    ByMasters
}