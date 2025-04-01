using System;
using System.Threading.Tasks;
using DynamicData;

namespace AE.PID.Client.Core;

public interface IFunctionLocationStore : IStore
{
    /// <summary>
    ///     Get the dynamic material locations
    /// </summary>
    IObservableCache<(FunctionLocation Location, Lazy<Task<ResolveResult<Function?>>> Function), ICompoundKey>
        FunctionLocations { get; }

    /// <summary>
    ///     Update the function locations that assigned to the document.
    /// </summary>
    /// <param name="locations"></param>
    /// <returns></returns>
    void Update(FunctionLocation[] locations);
}