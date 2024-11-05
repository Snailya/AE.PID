using System;
using System.Linq;
using System.Reactive.Disposables;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using AE.PID.Visio.Shared.Services;
using DynamicData;

namespace AE.PID.Visio.Services;

public class ToolService : DisposableBase, IToolService
{
    private readonly IVisioService _visioService;
    private readonly Lazy<IDisposable> _loader;
    private readonly SourceCache<Symbol, string> _symbols = new(t => t.Id);

    public ToolService(IVisioService visioService)
    {
        _visioService = visioService;
        // initialize the data
        _loader = new Lazy<IDisposable>(() => visioService.Masters.Value
            .Connect()
            .Transform(x => new Symbol
            {
                Id = x.BaseId,
                Name = x.Name
            })
            .PopulateInto(_symbols)
        );

        CleanUp.Add(Disposable.Create(() =>
        {
            if (_loader.IsValueCreated)
                _loader.Value.Dispose();
        }));
    }

    public IObservableCache<Symbol, string> Symbols => _symbols;

    public void Select(CompositeId id)
    {
        _visioService.SelectAndCenterView(id);
    }

    public void Select(Symbol[] items)
    {
        VisioService.SelectAndCenterView(items.Select(x => x.Id).ToArray());
    }

    public void Load()
    {
        var _ = _loader.Value;
    }
}