using AE.PID.Visio.Core.Models;
using DynamicData;

namespace AE.PID.Visio.Core.Interfaces;

public interface IToolService
{
    IObservableCache<Symbol, string> Symbols { get; }

    void Select(int id);
    void Select(Symbol[] items);
}