using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using AE.PID.Client.Core.VisioExt;
using AE.PID.Client.Infrastructure.VisioExt;
using AE.PID.Client.UI.Avalonia.Shared;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia.VisioExt;

public class SelectToolViewModel : ViewModelBase
{
    private readonly ReadOnlyObservableCollection<DocumentMasterViewModel> _symbols;
    private readonly IToolService _toolService;
    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public ReadOnlyObservableCollection<DocumentMasterViewModel> Symbols => _symbols;

    protected override void SetupStart()
    {
        base.SetupStart();

        _toolService.Load();
    }

    #region -- Commands --

    public ReactiveCommand<Unit, Unit> Confirm { get; set; }
    public ReactiveCommand<Unit, Unit> Cancel { get; set; }

    #endregion

    #region -- Constructors --

    public SelectToolViewModel(IToolService toolService)
    {
        _toolService = toolService;

        #region -- Commands --

        Confirm = ReactiveCommand.CreateRunInBackground(
            () =>
            {
                toolService.Select(Symbols.Where(x => x.IsSelected)
                    .Select<DocumentMasterViewModel, VisioMaster>(x => x.Source)
                    .ToArray());
            },
            backgroundScheduler: SchedulerManager.VisioScheduler);

        Cancel = ReactiveCommand.Create(() => { });

        #endregion

        #region -- Subscriptions --

        toolService.Masters.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(_ => IsLoading = true)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Transform(x => new DocumentMasterViewModel(x))
            .ObserveOn(RxApp.MainThreadScheduler)
            .SortAndBind(out _symbols, SortExpressionComparer<DocumentMasterViewModel>.Ascending(x => x.Name))
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