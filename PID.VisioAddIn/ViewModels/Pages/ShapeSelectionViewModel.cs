using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Controllers.Services;
using AE.PID.Models;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.ViewModels.Pages;

public class ShapeSelectionViewModel(ShapeSelector service) : ViewModelBase
{
    private SelectionType _selectionType = SelectionType.ById;
    private int _shapeId;
    private ReadOnlyObservableCollection<MasterViewModel> _masters;
    private bool _hasSelection = false;

    #region Read-Write Properties

    public SelectionType SelectionType
    {
        get => _selectionType;
        set => this.RaiseAndSetIfChanged(ref _selectionType, value);
    }

    public int ShapeId
    {
        get => _shapeId;
        set => this.RaiseAndSetIfChanged(ref _shapeId, value);
    }

    #endregion

    #region Read-Only Properties

    public OkCancelFeedbackViewModel OkCancelFeedbackViewModel { get; private set; } = new();

    public bool HasSelection
    {
        get => _hasSelection;
        private set => this.RaiseAndSetIfChanged(ref _hasSelection, value);
    }

    #endregion

    #region Output Properties

    public ReadOnlyObservableCollection<MasterViewModel> Masters => _masters;

    #endregion

    protected override void SetupCommands()
    {
        var canSelect = this.WhenAnyValue(
            x => x.SelectionType,
            x => x.ShapeId,
            x => x.HasSelection,
            (type, shapeId, hasSelection) =>
            {
                if (type == SelectionType.ById) return shapeId > 0;
                return hasSelection;
            });

        OkCancelFeedbackViewModel.Ok = ReactiveCommand.Create(() =>
            {
                if (SelectionType == SelectionType.ById)
                    ShapeSelector.SelectShapeById(_shapeId);
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
            .Transform(x => new MasterViewModel() { BaseId = x.BaseID, Name = x.Name })
            .Bind(out _masters)
            .DisposeMany()
            .Subscribe()
            .DisposeWith(d);

        service.MonitorChange()
            .DisposeWith(d);

        Masters.ToObservableChangeSet()
            .WhenPropertyChanged(i => i.IsChecked)
            .Select(_ => Masters.Any(x => x.IsChecked))
            .BindTo(this, x => x.HasSelection)
            .DisposeWith(d);
    }
}