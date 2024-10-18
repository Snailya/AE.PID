using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using DynamicData;
using DynamicData.Kernel;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class ConfirmSyncFunctionGroupsViewModel : ViewModelBase
{
    private readonly ReadOnlyObservableCollection<SyncFunctionGroupViewModel> _data;
    private readonly IFunctionService _fLocService;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isBusy;

    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    public ReadOnlyObservableCollection<SyncFunctionGroupViewModel> Data => _data;

    private async Task<IEnumerable<FunctionViewModel>> LoadAsync(int projectId,
        int functionId)
    {
        IsBusy = true;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        var result = new List<FunctionViewModel>();

        var response = await _fLocService.GetFunctionsAsync(projectId, functionId, cancellationToken);
        result.AddRange(response.Select(dto => new FunctionViewModel(dto)));

        IsBusy = false;
        return result;
    }

    #region -- Commands --

    public ReactiveCommand<Unit, (int, int, Function[])> Confirm { get; }
    public ReactiveCommand<Unit, Unit> Cancel { get; }

    #endregion

    #region -- Constructors --

    public ConfirmSyncFunctionGroupsViewModel(IFunctionService fLocService,
        IFunctionLocationStore functionLocationStore, int projectId, int functionId,
        CompositeId locationId)
    {
        _fLocService = fLocService;

        #region -- Commands --

        Confirm = ReactiveCommand.Create(() =>
            new ValueTuple<int, int, Function[]>(projectId, functionId,
                Data
                    .Where(x => x.Client != null)
                    .Select(x => x.Client!.Source).ToArray()));

        Cancel = ReactiveCommand.Create(() => { });

        #endregion

        ObservableChangeSet.Create<FunctionViewModel, string>(async cache =>
            {
                var items = await LoadAsync(projectId, functionId);
                cache.AddOrUpdate(items);
                return () => { };
            }, t => t.Code)
            .ObserveOn(RxApp.MainThreadScheduler)
            .FullJoin(functionLocationStore.FunctionLocations.Connect().Filter(x =>
                    x.ParentId.Equals(locationId)),
                x => x.Group,
                (server, client) => new SyncFunctionGroupViewModel(server.ValueOrDefault(), client.HasValue
                    ? new FunctionViewModel(new Function
                    {
                        Id = client.Value.FunctionId,
                        Code = client.Value.Group,
                        Name = client.Value.GroupName,
                        EnglishName = client.Value.GroupEnglishName,
                        Description = client.Value.Description
                    })
                    : null))
            .Bind(out _data)
            .Subscribe();
    }

    internal ConfirmSyncFunctionGroupsViewModel()
    {
        // Design
    }

    #endregion
}