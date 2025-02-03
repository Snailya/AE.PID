using System;
using System.Reactive.Subjects;
using DynamicData;

namespace AE.PID.Client.Core;

public interface IDataProvider
{
    /// <summary>
    ///     The logical location from which the detail data of the project is provided and stored.
    /// </summary>
    IObservable<ProjectLocation> ProjectLocation { get; }

    /// <summary>
    ///     A subject that receives the project location value that need to propagate back to the data source.
    /// </summary>
    Subject<ProjectLocation> ProjectLocationUpdater { get; }

    /// <summary>
    ///     The logical location from which the function is provided and stored.
    /// </summary>
    IObservableCache<FunctionLocation, ICompoundKey> FunctionLocations { get; }

    /// <summary>
    ///     A subject that receives the function location values that need to propagate back to the data source.
    /// </summary>
    Subject<FunctionLocation[]> FunctionLocationsUpdater { get; }

    /// <summary>
    ///     The logical location from which the detail data of the material is provided and stored.
    /// </summary>
    IObservableCache<MaterialLocation, ICompoundKey> MaterialLocations { get; }

    /// <summary>
    ///     A subject that receives the material location values that need to propagate back to the data source.
    /// </summary>
    Subject<MaterialLocation[]> MaterialLocationsUpdater { get; }

    /// <summary>
    ///     Get the adjacent location that connected to the location
    /// </summary>
    /// <returns></returns>
    ICompoundKey[] GetAdjacent(ICompoundKey compositeId);
}