using AE.PID.Client.Core.VisioExt.Models;
using DynamicData;

namespace AE.PID.Client.Core.VisioExt;

public interface IToolService : ILazyLoad
{
    /// <summary>
    ///     Get the symbols used by the documents.
    /// </summary>
    IObservableCache<VisioMaster, string> Masters { get; }

    /// <summary>
    ///     Select the shape by id.
    /// </summary>
    /// <param name="id"></param>
    void Select(VisioShapeId id);

    /// <summary>
    ///     Select the shapes by its masters.
    /// </summary>
    /// <param name="items"></param>
    void Select(VisioMaster[] items);
}