using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using AE.PID.Client.UI.Avalonia.Shared;
using AE.PID.Core;
using DynamicData;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public class ConfirmSyncFunctionGroupsViewModel : ViewModelBase
{
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

    public ObservableCollection<SyncFunctionGroupViewModel> Data { get; } = new();

    private async Task LoadAsync(int projectId,
        int functionId)
    {
        IsBusy = true;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        var response = await _fLocService.GetFunctionsAsync(projectId, functionId, cancellationToken);
        foreach (var function in response)
        {
            var viewmodel = Data.SingleOrDefault(x => x.Local?.Id == function.Id);
            if (viewmodel != null)
            {
                viewmodel.Remote = new FunctionViewModel(function);
            }
            else
            {
                viewmodel = new SyncFunctionGroupViewModel
                {
                    Remote = new FunctionViewModel(function)
                };
                Data.Add(viewmodel);
            }
        }

        IsBusy = false;
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
        ICompoundKey locationId)
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
                            Id = x.Remote!.Id!.Value,
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
                            Id = x.Remote!.Id!.Value,
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
        });

        Cancel = ReactiveCommand.Create(() => { });

        #endregion

        // for a confirmation page, the data should be immutable because if the data changed without user notified, the user would be confirmed with wrong data

        // firstly, get the current local data
        Data.AddRange(functionLocationStore.FunctionLocations.Items.Select(x => x.Location)
            .Where(x => x is { ParentId: not null })
            .Where(x => x.ParentId!.Equals(locationId)).Select(x =>
                new SyncFunctionGroupViewModel
                {
                    Local = new FunctionViewModel(x.FunctionId, x.GroupName, x.Group, x.GroupEnglishName, x.Description)
                }
            ));
        _ = LoadAsync(projectId, parentId);
    }


    internal ConfirmSyncFunctionGroupsViewModel()
    {
        // Design
    }

    #endregion
}