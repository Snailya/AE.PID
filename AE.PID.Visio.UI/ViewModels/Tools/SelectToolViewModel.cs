using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using AE.PID.Visio.Shared;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class SelectToolViewModel : ViewModelBase
{
    private readonly ReadOnlyObservableCollection<SymbolViewModel> _symbols;
    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public ReactiveCommand<Unit, Unit> Confirm { get; set; }
    public ReactiveCommand<Unit, Unit> Cancel { get; set; }

    public ReadOnlyObservableCollection<SymbolViewModel> Symbols => _symbols;

    #region -- Constructors --

    public SelectToolViewModel(IToolService toolService)
    {
        #region -- Commands --

        Confirm = ReactiveCommand.CreateRunInBackground(
            () =>
            {
                toolService.Select(Symbols.Where(x => x.IsSelected).Select<SymbolViewModel, Symbol>(x => x.Source)
                    .ToArray());
            },
            backgroundScheduler: SchedulerManager.VisioScheduler);

        #endregion

        #region -- Subscriptions --

        toolService.Symbols.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(_ => IsLoading = true)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Transform(x => new SymbolViewModel(x))
            .ObserveOn(RxApp.MainThreadScheduler)
            .SortAndBind(out _symbols, SortExpressionComparer<SymbolViewModel>.Ascending(x => x.Name))
            .Do(_ => IsLoading = false)
            .Subscribe();

        #endregion
    }

    internal SelectToolViewModel()
    {
        // Design
    }

    #endregion
}