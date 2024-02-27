using DynamicData;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace AE.PID.ViewModels;

public class DesignMaterialsControlViewModel(DesignMaterialService materialService) : ViewModelBase
{
    private ElementViewModel? _seed;
    private DesignMaterialViewModel? _selected;
    private ReadOnlyObservableCollection<DesignMaterialViewModel> _items = new([]);
    private IEnumerable<string> _columnNames = [];

    #region Read-Write Properties

    public ElementViewModel? Seed
    {
        get => _seed;
        set => this.RaiseAndSetIfChanged(ref _seed, value);
    }

    public DesignMaterialViewModel? Selected
    {
        get => _selected;
        set => this.RaiseAndSetIfChanged(ref _selected, value);
    }

    public ReactiveCommand<Unit, Unit>? Select { get; set; }

    #endregion

    public ReadOnlyObservableCollection<DesignMaterialViewModel> Items => _items;

    public IEnumerable<string> Columns
    {
        get => _columnNames;
        set => this.RaiseAndSetIfChanged(ref _columnNames, value);
    }

    protected override void SetupSubscriptions(CompositeDisposable d)
    {
        materialService.Materials
            .Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _items)
            .DisposeMany()
            .Subscribe()
            .DisposeWith(d);

        // reload items if the seed's name change
        this.WhenAnyValue(x => x.Seed)
            .WhereNotNull()
            .Select(x => x.Name)
            .DistinctUntilChanged()
            .Subscribe(x =>
            {
                if (x == null) return;

                Columns = materialService.ReloadMaterials(x);
            })
            .DisposeWith(d);
    }
}