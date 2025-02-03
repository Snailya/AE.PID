using System;
using System.Threading.Tasks;
using DynamicData;

namespace AE.PID.Client.Core;

public interface IMaterialLocationStore : IStore, ILazyLoad
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
    ///     Export the material locations to an Excel workbook.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    Task ExportAsWorkbook(string fileName);

    /// <summary>
    ///     Export the material locations as an embedded object in Visio.
    /// </summary>
    /// <returns></returns>
    Task ExportAsEmbeddedObject();
}