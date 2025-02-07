using System;
using System.Linq;
using System.Reactive.Disposables;
using AE.PID.Client.Core;
using AE.PID.Client.Core.VisioExt;
using AE.PID.Client.Core.VisioExt.Models;
using AE.PID.Client.Infrastructure;
using DynamicData;

namespace AE.PID.Client.VisioAddIn;

public class ToolService : DisposableBase, IToolService
{
    // 2025.02.05: 不在使用IVisioProvider，而是IDataProvider，以解决DI时只注册了IDataProvider而找不到IVisioDataProvider的问题。
    private readonly IDataProvider _dataProvider;
    private readonly Lazy<IDisposable>? _loader;
    private readonly SourceCache<VisioMaster, string> _masters = new(t => t.Id.BaseId);

    public ToolService(IDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
        if (dataProvider is not IVisioDataProvider visioDataProvider) return;

        // initialize the data
        _loader = new Lazy<IDisposable>(() => visioDataProvider.Masters.Value
            .Connect()
            .PopulateInto(_masters));

        CleanUp.Add(Disposable.Create(() =>
        {
            if (_loader.IsValueCreated)
                _loader.Value.Dispose();
        }));
    }

    public IObservableCache<VisioMaster, string> Masters => _masters;

    public void Select(VisioShapeId id)
    {
        if (_dataProvider is ISelectable selectable)
            selectable.Select([id]);
    }

    public void Select(VisioMaster[] items)
    {
        if (_dataProvider is ISelectable selectable)
            selectable.Select(items.Select(x => x.Id).ToArray<ICompoundKey>());
    }

    public void Load()
    {
        var _ = _loader?.Value;
    }
}