using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using AE.PID.Controllers.Services;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class ShapeSelectionViewModel : ReactiveObject
{
    private bool _isByIdChecked = true;
    private bool _isByMasterChecked;
    private int _shapeId;

    public ShapeSelectionViewModel()
    {
        Masters = new ObservableCollection<MasterViewModel>(Selector.GetMastersSource());

        var canSelectShapeById = this.WhenAnyValue(
            x => x.IsByIdChecked,
            x => x.ShapeId,
            (isChecked, id) => isChecked && id > 0);
        var selectShapeById = ReactiveCommand.Create(() => Selector.SelectShapeById(_shapeId), canSelectShapeById);
        selectShapeById.ThrownExceptions.Subscribe(error => MessageBox.Show(error.Message));

        // create a by masters command executable only when masters in list selected and mode is by master
        var canSelectShapesByMaster = this.WhenAnyValue(
                x => x.IsByMastersChecked)
            .CombineLatest(
                Masters.ToObservableChangeSet().WhenPropertyChanged(x => x.IsChecked)
                    .Select(_ => Masters.Any(x => x.IsChecked)),
                (isChecked, hasSelection) => isChecked & hasSelection);
        var selectShapesByMasters =
            ReactiveCommand.Create(
                () => Selector.SelectShapesByMasters(Masters.Where(x => x.IsChecked).Select(x => x.BaseId)),
                canSelectShapesByMaster);
        selectShapesByMasters.ThrownExceptions.Subscribe(error => MessageBox.Show(error.Message));

        // todo: don't know if this is the better way to execute a set of command if any of them can execute. or to say bind two command to a button.
        Select = ReactiveCommand.Create(() => { },
            canSelectShapeById.CombineLatest(canSelectShapesByMaster, (canById, canByMaster) => canById | canByMaster));
        Select.InvokeCommand(selectShapeById);
        Select.InvokeCommand(selectShapesByMasters);

        // close window command
        Cancel = ReactiveCommand.Create(() => { });
    }

    /// <summary>
    ///     The master options for use to choose in by master mode.
    /// </summary>
    public ObservableCollection<MasterViewModel> Masters { get; }

    /// <summary>
    ///     Whether is in by id mode.
    /// </summary>
    public bool IsByIdChecked
    {
        get => _isByIdChecked;
        set => this.RaiseAndSetIfChanged(ref _isByIdChecked, value);
    }

    /// <summary>
    ///     Whether is in by master mode
    /// </summary>
    public bool IsByMastersChecked
    {
        get => _isByMasterChecked;
        set => this.RaiseAndSetIfChanged(ref _isByMasterChecked, value);
    }

    /// <summary>
    ///     The shape id display and edit in input box by user.
    /// </summary>
    public int ShapeId
    {
        get => _shapeId;
        set => this.RaiseAndSetIfChanged(ref _shapeId, value);
    }

    /// <summary>
    ///     Execute select command which might disabled by can execute.
    /// </summary>
    public ReactiveCommand<Unit, Unit> Select { get; }

    /// <summary>
    ///     Close window command.
    /// </summary>
    public ReactiveCommand<Unit, Unit> Cancel { get; }
}