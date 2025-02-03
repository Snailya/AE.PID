using System;
using System.Linq;
using System.Reactive.Disposables;
using AE.PID.Client.Core.VisioExt;
using AE.PID.Client.Core.VisioExt.Models;
using AE.PID.Client.Infrastructure;
using DynamicData;

namespace AE.PID.Client.VisioAddIn;

public class ToolService : DisposableBase, IToolService
{
    private readonly Lazy<IDisposable> _loader;
    private readonly SourceCache<VisioMaster, string> _masters = new(t => t.Id.BaseId);
    private readonly IVisioDataProvider _visioService;

    public ToolService(IVisioDataProvider visioService)
    {
        _visioService = visioService;
        // initialize the data
        _loader = new Lazy<IDisposable>(() => visioService.Masters.Value
            .Connect()
            .PopulateInto(_masters)
        );

        CleanUp.Add(Disposable.Create(() =>
        {
            if (_loader.IsValueCreated)
                _loader.Value.Dispose();
        }));
    }

    public IObservableCache<VisioMaster, string> Masters => _masters;

    public void Select(VisioShapeId id)
    {
        _visioService.Select([id]);
    }

    public void Select(VisioMaster[] items)
    {
        _visioService.Select(items.Select(x => x.Id).ToArray());
    }

    public void Load()
    {
        var _ = _loader.Value;
    }
}