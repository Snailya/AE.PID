using AE.PID.Visio.Core.Models;
using DynamicData;

namespace AE.PID.Visio.Core.Interfaces;

public interface IToolService : ILazyLoad
{
    /// <summary>
    ///     Get the symbols used by the documents.
    /// </summary>
    IObservableCache<Symbol, string> Symbols { get; }

    /// <summary>
    ///     Select the shape by id.
    /// </summary>
    /// <param name="id"></param>
    void Select(CompositeId id);

    /// <summary>
    ///     Select the shapes by its master symbol.
    /// </summary>
    /// <param name="items"></param>
    void Select(Symbol[] items);
}