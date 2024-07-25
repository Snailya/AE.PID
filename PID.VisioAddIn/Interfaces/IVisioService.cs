using System;
using DynamicData;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Interfaces;

public interface IVisioService
{
    /// <summary>
    ///     Indicate whether it is busy
    /// </summary>
    public IObservable<bool> IsLoading { get; }

    /// <summary>
    ///     Masters of the active document.
    /// </summary>
    public IObservableCache<IVMaster, string> Masters { get; }

    void OpenDocument(string fullName);
    bool CloseDocumentIfOpened(string fullName);

    /// <summary>
    ///     Create selection in active page for shapes of specified masters.
    /// </summary>
    /// <param name="baseIds"></param>
    bool SelectShapesByMasters(string[] baseIds);

    /// <summary>
    ///     Create a selection in active page by specified shape id.
    /// </summary>
    /// <param name="id"></param>
    bool SelectShapeById(int id);
}