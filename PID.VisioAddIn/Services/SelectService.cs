using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Services;

/// <summary>
///     A selection service used for enhance user selection.
/// </summary>
public class SelectService : ServiceBase
{
    private readonly SourceCache<IVMaster, int> _masters = new(t => t.ID);
    private readonly Page _page;

    #region Constructors

    public SelectService(Page page)
    {
        Contract.Assert(page.Document.Type == VisDocumentTypes.visTypeDrawing,
            "Selection tool can only be used on drawing");

        _page = page;
    }

    #endregion

    #region Output Properties

    public IObservableCache<IVMaster, int> Masters => _masters.AsObservableCache();

    #endregion

    public override void Start()
    {
        if (CleanUp.Any()) return;

        // when a shape's property is modified, it will raise up FormulaChanged event, so that the modification could be captured to emit as a new value
        Observable
            .FromEvent<EDocument_MasterAddedEventHandler, Master>(
                handler => _page.Document.MasterAdded += handler,
                handler => _page.Document.MasterAdded -= handler)
            .Subscribe(master =>
            {
                if (master != null) _masters.AddOrUpdate(master);
            })
            .DisposeWith(CleanUp);

        // when a new shape is added to the page, it could be captured using ShapeAdded event
        Observable.FromEvent<EDocument_BeforeMasterDeleteEventHandler, Master>(
                handler => _page.Document.BeforeMasterDelete += handler,
                handler => _page.Document.BeforeMasterDelete -= handler)
            .Subscribe(master => { _masters.Remove(master); })
            .DisposeWith(CleanUp);
    }

    public void LoadMasters()
    {
        _masters.AddOrUpdate(_page.Document.Masters.OfType<IVMaster>());
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