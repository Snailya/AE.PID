using System;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using DynamicData;

namespace AE.PID.Visio.UI.Design.Services;

public class MoqToolService : IToolService
{
    public IObservableCache<Symbol, string> Symbols { get; }

    public void Select(int id)
    {
        throw new NotImplementedException();
    }

    public void Select(CompositeId id)
    {
        throw new NotImplementedException();
    }

    public void Select(Symbol[] items)
    {
        throw new NotImplementedException();
    }

    public void Load()
    {
        throw new NotImplementedException();
    }
}