using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Services;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class SelectToolPageViewModel(ShapeSelector service) : ViewModelBase
{
    private bool _hasSelection;
    private ReadOnlyObservableCollection<MasterOptionViewModel> _masters = new([]);
    private SelectionMode _mode = SelectionMode.ById;
    private int _shapeId;

    #region Output Properties

    public ReadOnlyObservableCollection<MasterOptionViewModel> Masters => _masters;

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
                if (Mode == SelectionMode.ById)
                    service.SelectShapeById(_shapeId);
                else
                    ShapeSelector.SelectShapesByMasters(_masters.Where(x => x.IsChecked).Select(x => x.BaseId));
            },
            canSelect);

        OkCancelFeedbackViewModel.Cancel = ReactiveCommand.Create(() => { });
    }


    protected override void SetupSubscriptions(CompositeDisposable d)
    {
        service.Masters
            .Connect()
            .Transform(x => new MasterOptionViewModel(x))
            .Sort(SortExpressionComparer<MasterOptionViewModel>.Ascending(t => t.Name))
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
        ThisAddIn.Dispatcher!.InvokeAsync(service.Initialize);
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