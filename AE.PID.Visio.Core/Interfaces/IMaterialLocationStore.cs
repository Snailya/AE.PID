using AE.PID.Visio.Core.Models;
using DynamicData;

namespace AE.PID.Visio.Core.Interfaces;

public interface IMaterialLocationStore : IStore,ILazyLoad
{
    /// <summary>
    ///     Get the dynamic material locations
    /// </summary>
    IObservableCache<MaterialLocation, CompositeId> MaterialLocations { get; }
    
    /// <summary>
    /// Locate the material on the drawing if there exists one.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task Locate(CompositeId id);

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