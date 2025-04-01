using System;
using System.Threading.Tasks;
using DynamicData;

namespace AE.PID.Client.Core;

public interface IMaterialLocationStore : IStore
{
    /// <summary>
    ///     Get the dynamic material locations
    /// </summary>
    IObservableCache<(MaterialLocation Location, Lazy<Task<ResolveResult<Material?>>> Material), ICompoundKey>
        MaterialLocations { get; }

    /// <summary>
    ///     Update the material locations that assigned to the document.
    /// </summary>
    /// <param name="locations"></param>
    /// <returns></returns>
    void Update(MaterialLocation[] locations);

    /// <summary>
    ///     Locate the material on the drawing if there exists one.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task Locate(ICompoundKey id);

    /// <summary>
    ///     Convert to the part list and save as an Excel file or as an embedded object.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    Task ExportPartList(string? fileName = null);

    
    /// <summary>
    /// Convert to the procurement list and save as an Excel file.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    Task ExportProcurementList(string fileName);
}