using AE.PID.Visio.Core.Models;
using DynamicData;

namespace AE.PID.Visio.Core.Interfaces;

public interface IFunctionLocationStore : IStore
{
    /// <summary>
    ///     Get the dynamic locations
    /// </summary>
    IObservableCache<FunctionLocation, CompositeId> FunctionLocations { get; }

    /// <summary>
    ///     Update the location with the function.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="function"></param>
    void Update(CompositeId id, Function function);

    /// <summary>
    ///     Get the function location by its id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    FunctionLocation? Find(CompositeId id);
}