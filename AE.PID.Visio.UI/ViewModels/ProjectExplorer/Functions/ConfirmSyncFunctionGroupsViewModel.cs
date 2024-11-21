using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using AE.PID.Core.Models;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using DynamicData;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class ConfirmSyncFunctionGroupsViewModel : ViewModelBase
{
    private readonly ReadOnlyObservableCollection<SyncFunctionGroupViewModel> _data;
    private readonly IFunctionService _fLocService;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isBusy;


    private IEnumerable<FunctionViewModel> _remote = [];
    private SyncFunctionGroupViewModel? _selected;

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

    public SyncFunctionGroupViewModel? Selected
    {
        get => _selected;
        set => this.RaiseAndSetIfChanged(ref _selected, value);
    }

    #endregion

    #region -- Constructors --

    public ConfirmSyncFunctionGroupsViewModel(IFunctionService fLocService,
        IFunctionLocationStore functionLocationStore, int projectId, int parentId,
        CompositeId locationId)
    {
        _fLocService = fLocService;

        #region -- Commands --

        Confirm = ReactiveCommand.Create(() =>
            {
                var functions = Data.Where(x => x.IsSelected).Select(x =>
                {
                    return x.Status switch
                    {
                        SyncStatus.Added =>
                            new Function
                            {
                                Id = 0,
                                Type = FunctionType.FunctionGroup,
                                Code = x.Local!.Code,
                                Name = x.Local.Name,
                                EnglishName = x.Local.EnglishName,
                                Description = x.Local.Description,
                                IsEnabled = true
                            },
                        SyncStatus.Deleted =>
                            new Function
                            {
                                Id = x.Remote!.Id,
                                Type = FunctionType.FunctionGroup,
                                Code = x.Local!.Code,
                                Name = x.Local.Name,
                                EnglishName = x.Local.EnglishName,
                                Description = x.Local.Description,
                                IsEnabled = false
                            },
                        SyncStatus.Modified or SyncStatus.Unchanged =>
                            new Function
                            {
                                Id = x.Remote!.Id,
                                Type = FunctionType.FunctionGroup,
                                Code = x.Local!.Code,
                                Name = x.Local.Name,
                                EnglishName = x.Local.EnglishName,
                                Description = x.Local.Description,
                                IsEnabled = true
                            },
                        _ => throw new ArgumentOutOfRangeException()
                    };
                }).ToArray();

                return new ValueTuple<int, int, Function[]>(projectId, parentId, functions);
            }
        );

        Cancel = ReactiveCommand.Create(() => { });

        #endregion

        var observeRemote = ObservableChangeSet.Create<FunctionViewModel>(async cache =>
        {
            var items = await LoadAsync(projectId, parentId);
            cache.AddRange(items);
            return () => { };
        });

        var observeLocal = functionLocationStore.FunctionLocations.Connect().Filter(x =>
            x.ParentId.Equals(locationId));

        observeRemote.AddKey(x => x.Id)
            .LeftJoin(observeLocal.ChangeKey(x => x.FunctionId), x => x.FunctionId,
                (x, matchId) => new { Remote = x, Local = matchId })
            .ChangeKey(x => x.Remote.Code).FullJoin(observeLocal, x => x.Group, (matchId, matchCode) =>
            {
                if (matchId.HasValue)
                {
                    if (matchId.Value.Local.HasValue)
                        return new SyncFunctionGroupViewModel(matchId.Value.Remote, new FunctionViewModel(new Function
                        {
                            Id = matchId.Value.Local.Value.FunctionId,
                            Type = FunctionType.FunctionGroup,
                            Code = matchId.Value.Local.Value.Group,
                            Name = matchId.Value.Local.Value.GroupName,
                            EnglishName = matchId.Value.Local.Value.GroupEnglishName,
                            Description = matchId.Value.Local.Value.Description
                        }));

                    if (matchCode.HasValue)
                        return new SyncFunctionGroupViewModel(matchId.Value.Remote, new FunctionViewModel(new Function
                        {
                            Id = 0,
                            Type = FunctionType.FunctionGroup,
                            Code = matchCode.Value.Group,
                            Name = matchCode.Value.GroupName,
                            EnglishName = matchCode.Value.GroupEnglishName,
                            Description = matchCode.Value.Description
                        }));

                    return new SyncFunctionGroupViewModel(matchId.Value.Remote, null);
                }

                return new SyncFunctionGroupViewModel(null, new FunctionViewModel(new Function
                {
                    Id = 0,
                    Type = FunctionType.FunctionGroup,
                    Code = matchCode.Value.Group,
                    Name = matchCode.Value.GroupName,
                    EnglishName = matchCode.Value.GroupEnglishName,
                    Description = matchCode.Value.Description
                }));
            })
            .Bind(out _data)
            .Subscribe();
    }

    internal ConfirmSyncFunctionGroupsViewModel()
    {
        // Design
    }

    #endregion
}