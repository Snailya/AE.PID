using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using AE.PID.Client.Core.VisioExt;
using DynamicData;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Client.VisioAddIn;

public class VisioDocumentMonitor : IDisposable
{
    private readonly Lazy<IDisposable> _loader;
    private readonly SourceCache<VisioShape, VisioShapeId> _shapes = new(x => x.Id);

    public VisioDocumentMonitor(Document document, IScheduler scheduler)
    {
        _loader = new Lazy<IDisposable>(() => document.ToShapeChangeSet()
            .SubscribeOn(scheduler)
            .PopulateInto(_shapes));

        // because the masters are always in small amount, no need to control the load behavior externally
        Masters = new Lazy<IObservableCache<VisioMaster, string>>(() => document.ToMasterChangeSet()
            .SubscribeOn(scheduler)
            .AsObservableCache());
    }

    public IObservableCache<VisioShape, VisioShapeId> Shapes
    {
        get
        {
            if (!_loader.IsValueCreated)
                _ = _loader.Value; // 触发加载（线程安全）
            return _shapes.AsObservableCache();
        }
    }

    public Lazy<IObservableCache<VisioMaster, string>> Masters { get; set; }

    public void Dispose()
    {
        if (_loader.IsValueCreated)
            _loader.Value.Dispose();

        _shapes.Dispose();

        if (Masters is { IsValueCreated: true, Value: IDisposable disposable }) disposable.Dispose();
    }
}