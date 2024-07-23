using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using AE.PID.Services;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using ReactiveUI;
using Splat;
using TaskStatus = AE.PID.Core.Models.TaskStatus;

namespace AE.PID.ViewModels;

public class SelectToolPageViewModel(SelectService? service = null) : ViewModelBase
{
    private readonly SelectService _service = service ?? Locator.Current.GetService<SelectService>()!;
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

        OkCancelFeedbackViewModel.Ok = ReactiveCommand.CreateFromObservable(() =>
                Observable.Start(() =>
                    {
                        return Task.FromResult(Mode == SelectionMode.ById
                            ? SelectService.SelectShapeById(_shapeId)
                            : SelectService.SelectShapesByMasters(
                                Masters.Where(x => x.IsChecked).Select(x => x.BaseId).ToArray()));
                    }, AppScheduler.VisioScheduler)
                    .ObserveOn(AppScheduler.VisioScheduler)
                    .SelectMany(x => x)
                    .Do(x =>
                    {
                        if (x == false) WindowManager.ShowDialog("没有找到", MessageBoxButton.OK);
                    })
                    .Select(_ => Unit.Default) // Ensure result is processed on the main thread
            , canSelect);

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
                    _service.LoadMasters();
                }
                catch (Exception e)
                {
                    observer.OnError(e);
                }

                observer.OnNext(TaskStatus.RanToCompletion);
                observer.OnCompleted();

                return () => { };
            })
            .SubscribeOn(AppScheduler.VisioScheduler!)
            .Select(x => x == TaskStatus.Running)
            .ObserveOn(AppScheduler.UIScheduler)
            .ToProperty(this, x => x.IsMastersLoading, out _isMastersLoading)
            .DisposeWith(d);

        _service.Masters
            .Connect()
            .Transform(x => new MasterOptionViewModel(x))
            .Sort(SortExpressionComparer<MasterOptionViewModel>.Ascending(t => t.Name))
            .ObserveOn(AppScheduler.UIScheduler)
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
        AppScheduler.VisioScheduler!.Schedule(() => _service.Start());
    }

    protected override void SetupDeactivate()
    {
        AppScheduler.VisioScheduler!.Schedule(() => _service.Stop());
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