using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AE.PID.Visio.Core.Interfaces;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class SelectFunctionViewModel : ViewModelBase
{
    private readonly IFunctionService _functionService;
    private CancellationTokenSource? _cancellationTokenSource;
    private ReadOnlyObservableCollection<FunctionViewModel> _data;
    private bool _isBusy;
    private FunctionViewModel? _selectedFunctionZone;

    private async Task<IEnumerable<FunctionViewModel>> LoadAsync(int? projectId = null)
    {
        IsBusy = true;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        var result = new List<FunctionViewModel>();
        if (projectId.HasValue)
        {
            var response = await _functionService.GetFunctionsAsync(projectId.Value, null, cancellationToken);
            result.AddRange(response.Select(dto => new FunctionViewModel(dto)));
        }
        else
        {
            var response = await _functionService.GetStandardFunctionGroupsAsync(cancellationToken);
            result.AddRange(response.Select(dto => new FunctionViewModel(dto)).OrderBy(x => x.Code));
        }

        IsBusy = false;

        return result;
    }

    #region -- Public Properties --

    public FunctionViewModel? SelectedFunctionZone
    {
        get => _selectedFunctionZone;
        set => this.RaiseAndSetIfChanged(ref _selectedFunctionZone, value);
    }

    public ReadOnlyObservableCollection<FunctionViewModel> Data
    {
        get => _data;
        set => this.RaiseAndSetIfChanged(ref _data, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    #endregion

    #region -- Constructors --

    public SelectFunctionViewModel(IFunctionService functionService, int? projectId = null)
    {
        _functionService = functionService;

        #region -- Commands --

        Confirm = ReactiveCommand.Create(() => SelectedFunctionZone);
        Cancel = ReactiveCommand.Create(() => { });

        #endregion

        Observable.StartAsync(async () => await LoadAsync(projectId))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(items =>
            {
                Data = new ReadOnlyObservableCollection<FunctionViewModel>(
                    new ObservableCollection<FunctionViewModel>(items));
            });
    }

    internal SelectFunctionViewModel()
    {
        // Design
    }

    #endregion

    #region -- Commands --

    public ReactiveCommand<Unit, FunctionViewModel?> Confirm { get; set; }
    public ReactiveCommand<Unit, Unit> Cancel { get; set; }

    #endregion
}