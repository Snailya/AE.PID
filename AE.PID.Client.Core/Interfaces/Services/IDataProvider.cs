using System;
using DynamicData;

namespace AE.PID.Client.Core;

public interface IDataProvider
{
    /// <summary>
    ///     The logical location from which the detail data of the project is provided and stored.
    /// </summary>
    IObservable<ProjectLocation> ProjectLocation { get; }

    /// <summary>
    ///     The logical location from which the function is provided and stored.
    /// </summary>
    IObservableCache<FunctionLocation, ICompoundKey> FunctionLocations { get; }

    /// <summary>
    ///     The logical location from which the detail data of the material is provided and stored.
    /// </summary>
    IObservableCache<MaterialLocation, ICompoundKey> MaterialLocations { get; }

    /// <summary>
    ///     A subject that receives the project location value that needs to propagate back to the data source.
    /// </summary>
    void UpdateProjectLocation(ProjectLocation projectLocation);

    /// <summary>
    ///     A subject that receives the function location values that need to propagate back to the data source.
    /// </summary>
    void UpdateFunctionLocations(FunctionLocation[] functionLocations);

    /// <summary>
    ///     A subject that receives the material location values that need to propagate back to the data source.
    /// </summary>
    void UpdateMaterialLocations(MaterialLocation[] materialLocations);

    /// <summary>
    ///     Get the adjacent location that connected to the location
    /// </summary>
    /// <returns></returns>
    ICompoundKey[] GetAdjacent(ICompoundKey id);
}