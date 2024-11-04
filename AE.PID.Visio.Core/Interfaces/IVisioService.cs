using AE.PID.Visio.Core.Models;
using DynamicData;

namespace AE.PID.Visio.Core.Interfaces;

public interface IVisioService
{
    /// <summary>
    ///     Get the document property by prop name.
    /// </summary>
    /// <param name="propName"></param>
    /// <returns></returns>
    string? GetDocumentProperty(string propName);

    /// <summary>
    ///     Get the document property by prop name.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="propName"></param>
    /// <returns></returns>
    string? GetPageProperty(int id, string propName);

    /// <summary>
    ///     Get the document property by prop name.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="propName"></param>
    /// <returns></returns>
    string? GetShapeProperty(CompositeId id, string propName);

    /// <summary>
    ///     Update document properties
    /// </summary>
    /// <param name="patches"></param>
    void UpdateDocumentProperties(IEnumerable<ValuePatch> patches);

    /// <summary>
    ///     Update page properties
    /// </summary>
    /// <param name="id"></param>
    /// <param name="patches"></param>
    void UpdatePageProperties(int id, IEnumerable<ValuePatch> patches);

    /// <summary>
    ///     Update shape properties
    /// </summary>
    /// <param name="id"></param>
    /// <param name="patches"></param>
    void UpdateShapeProperties(CompositeId id, IEnumerable<ValuePatch> patches);
    
    /// <summary>
    ///     Insert the data as embedded Excel sheet at the active page.
    /// </summary>
    /// <param name="dataArray"></param>
    void InsertAsExcelSheet(string[,] dataArray);
    
    /// <summary>
    ///     Select the shape with specified id and make it view center.
    /// </summary>
    /// <param name="id"></param>
    void SelectAndCenterView(CompositeId id);

    #region -- Models --

    /// <summary>
    ///     Get the function locations from the document.
    /// </summary>
    Lazy<IObservableCache<FunctionLocation, CompositeId>> FunctionLocations { get; }

    /// <summary>
    ///     Get the material locations from the document
    /// </summary>
    Lazy<IObservableCache<MaterialLocation, CompositeId>> MaterialLocations { get; }

    /// <summary>
    ///     Get the symbols from the document
    /// </summary>
    Lazy<IObservableCache<Symbol, string>> Symbols { get; }

    #endregion

    /// <summary>
    /// Get the shape that connected to the shape
    /// </summary>
    /// <returns></returns>
    CompositeId[] GetAdjacent(CompositeId compositeId);
}