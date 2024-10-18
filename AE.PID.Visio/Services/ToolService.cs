using System.Linq;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using AE.PID.Visio.Shared.Services;
using DynamicData;

namespace AE.PID.Visio.Services;

public class ToolService(IVisioService visioService) : DisposableBase, IToolService
{
    public IObservableCache<Symbol, string> Symbols { get; } = visioService.Symbols.Value;

    public void Select(int id)
    {
        VisioService.SelectAndCenterView(id);
    }

    public void Select(Symbol[] items)
    {
        VisioService.SelectAndCenterView(items.Select(x => x.Id).ToArray());
    }
}