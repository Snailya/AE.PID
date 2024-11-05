using AE.PID.Visio.Core.Exceptions;
using AE.PID.Visio.Core.Models;
using DynamicData;

namespace AE.PID.Visio.Core.Interfaces;

public interface IVisioService
{
    /// <summary>
    ///     Get the shape that connected to the shape
    /// </summary>
    /// <returns></returns>
    CompositeId[] GetAdjacent(CompositeId compositeId);

    #region -- Interactions --

    /// <summary>
    ///     Insert the data as embedded Excel sheet at the active page.
    /// </summary>
    /// <param name="dataArray"></param>
    void InsertAsExcelSheet(string[,] dataArray);

    /// <summary>
    ///     Select the shape with specified id and make it view center.
    /// </summary>
    /// <param name="id"></param>
    /// <exception cref="ShapeNotExistException">If there is no shape that matches the id.</exception>
    void SelectAndCenterView(CompositeId id);

    #endregion

    #region -- Shape Sheet --

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

    #endregion

    #region -- Models --

    /// <summary>
    ///     Get the shapes from the document.
    ///     Because the load shape takes a lot of time, so it is defined as Lazy.
    /// </summary>
    Lazy<IObservableCache<VisioShape, CompositeId>> Shapes { get; }

    /// <summary>
    ///     Get the symbols from the document
    /// </summary>
    Lazy<IObservableCache<VisioMaster, string>> Masters { get; }

    /// <summary>
    ///     Get the shape as function location
    /// </summary>
    FunctionLocation ToFunctionLocation(VisioShape shape);

    /// <summary>
    ///     Convert the shape as material location
    /// </summary>
    MaterialLocation ToMaterialLocation(VisioShape shape);

    #endregion
}