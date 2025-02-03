using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using AE.PID.Client.Core.VisioExt;
using AE.PID.Client.Core.VisioExt.Models;
using AE.PID.Client.Infrastructure.VisioExt;
using AE.PID.UI.Shared;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.UI.Avalonia.VisioExt;

public class SelectToolViewModel : ViewModelBase
{
    private readonly ReadOnlyObservableCollection<SymbolViewModel> _symbols;
    private readonly IToolService _toolService;
    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public ReactiveCommand<Unit, Unit> Confirm { get; set; }
    public ReactiveCommand<Unit, Unit> Cancel { get; set; }

    public ReadOnlyObservableCollection<SymbolViewModel> Symbols => _symbols;

    protected override void SetupStart()
    {
        base.SetupStart();

        _toolService.Load();
    }

    #region -- Constructors --

    public SelectToolViewModel(IToolService toolService)
    {
        _toolService = toolService;

        #region -- Commands --

        Confirm = ReactiveCommand.CreateRunInBackground(
            () =>
            {
                toolService.Select(Symbols.Where(x => x.IsSelected).Select<SymbolViewModel, VisioMaster>(x => x.Source)
                    .ToArray());
            },
            backgroundScheduler: SchedulerManager.VisioScheduler);

        #endregion

        #region -- Subscriptions --

        toolService.Masters.Connect()
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