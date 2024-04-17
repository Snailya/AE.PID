using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Controllers.Services;

/// <summary>
///     A selection service used for enhance user selection.
/// </summary>
public class ShapeSelector : IDisposable
{
    private readonly CompositeDisposable _cleanUp = new();
    private readonly SourceCache<IVMaster, int> _masters = new(t => t.ID);
    private readonly Page _page;

    #region Constructors

    public ShapeSelector(Page page)
    {
        Contract.Assert(page.Document.Type == VisDocumentTypes.visTypeDrawing,
            "Selection tool can only be used on drawing");

        _page = page;

        // when a shape's property is modified, it will raise up FormulaChanged event, so that the modification could be captured to emit as a new value
        Observable
            .FromEvent<EDocument_MasterAddedEventHandler, Master>(
                handler => page.Document.MasterAdded += handler,
                handler => page.Document.MasterAdded -= handler)
            // switch to background thread
            .ObserveOn(ThreadPoolScheduler.Instance)
            .Subscribe(master =>
            {
                if (master != null) _masters.AddOrUpdate(master);
            })
            .DisposeWith(_cleanUp);

        // when a new shape is add to the page, it could be captured using ShapeAdded event
        Observable.FromEvent<EDocument_BeforeMasterDeleteEventHandler, Master>(
                handler => page.Document.BeforeMasterDelete += handler,
                handler => page.Document.BeforeMasterDelete -= handler)
            // switch to background thread
            .ObserveOn(ThreadPoolScheduler.Instance)
            .Subscribe(master => { _masters.Remove(master); })
            .DisposeWith(_cleanUp);

        // initialize items by get all items from current page
        _masters.AddOrUpdate(page.Document.Masters.OfType<IVMaster>());
    }

    #endregion

    #region Output Properties

    public IObservableCache<IVMaster, int> Masters => _masters.AsObservableCache();

    #endregion

    public void Dispose()
    {
        _cleanUp.Dispose();
    }

    /// <summary>
    ///     Create selection in active page for shapes of specified masters, it not work maybe to use layer instead.
    /// </summary>
    /// <param name="baseIds"></param>
    public static void SelectShapesByMasters(IEnumerable<string> baseIds)
    {
        var masters = Globals.ThisAddIn.Application.ActiveDocument.Masters.OfType<IVMaster>()
            .Where(x => baseIds.Contains(x.BaseID));

        var shapeIds = new List<int>();
        foreach (var master in masters)
        {
            Globals.ThisAddIn.Application.ActivePage.CreateSelection(VisSelectionTypes.visSelTypeByMaster,
                VisSelectMode.visSelModeSkipSuper, master).GetIDs(out var shapeIdsPerMaster);
            shapeIds.AddRange(shapeIdsPerMaster.OfType<int>());
        }

        var selection = Globals.ThisAddIn.Application.ActivePage.CreateSelection(VisSelectionTypes.visSelTypeEmpty);
        foreach (var id in shapeIds)
            selection.Select(Globals.ThisAddIn.Application.ActivePage.Shapes.ItemFromID[id],
                (short)VisSelectArgs.visSelect);
        Globals.ThisAddIn.Application.ActiveWindow.Selection = selection;
    }

    /// <summary>
    ///     Create a selection in active page by specified shape id.
    /// </summary>
    /// <param name="id"></param>
    public void SelectShapeById(int id)
    {
        var shape = _page.Shapes.OfType<Shape>().SingleOrDefault(x => x.ID == id);
        if (shape == null) return;
        
        // select and center screen
        _page.Application.ActiveWindow.Select(shape, (short)VisSelectArgs.visSelect);
        _page.Application.ActiveWindow.CenterViewOnShape(shape, VisCenterViewFlags.visCenterViewSelectShape);
    }
}