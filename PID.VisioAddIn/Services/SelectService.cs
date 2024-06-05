using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Services;

/// <summary>
///     A selection service used for enhance user selection.
/// </summary>
public class SelectService : PageServiceBase
{
    private readonly SourceCache<IVMaster, int> _masters = new(t => t.ID);

    #region Output Properties

    public IObservableCache<IVMaster, int> Masters => _masters.AsObservableCache();

    #endregion

    public override void Start()
    {
        if (CleanUp.Any()) return;

        // when a shape's property is modified, it will raise up FormulaChanged event, so that the modification could be captured to emit as a new value
        Observable
            .FromEvent<EDocument_MasterAddedEventHandler, Master>(
                handler => Globals.ThisAddIn.Application.ActivePage.Document.MasterAdded += handler,
                handler => Globals.ThisAddIn.Application.ActivePage.Document.MasterAdded -= handler)
            .Subscribe(master =>
            {
                if (master != null) _masters.AddOrUpdate(master);
            })
            .DisposeWith(CleanUp);

        // when a new shape is added to the page, it could be captured using ShapeAdded event
        Observable.FromEvent<EDocument_BeforeMasterDeleteEventHandler, Master>(
                handler => Globals.ThisAddIn.Application.ActivePage.Document.BeforeMasterDelete += handler,
                handler => Globals.ThisAddIn.Application.ActivePage.Document.BeforeMasterDelete -= handler)
            .Subscribe(master => { _masters.Remove(master); })
            .DisposeWith(CleanUp);
    }

    public void LoadMasters()
    {
        _masters.AddOrUpdate(Globals.ThisAddIn.Application.ActivePage.Document.Masters.OfType<IVMaster>());
    }

    /// <summary>
    ///     Create selection in active page for shapes of specified masters, it not work maybe to use layer instead.
    /// </summary>
    /// <param name="baseIds"></param>
    public static bool SelectShapesByMasters(IEnumerable<string> baseIds)
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

        return selection.Count != 0;
    }

    /// <summary>
    ///     Create a selection in active page by specified shape id.
    /// </summary>
    /// <param name="id"></param>
    public static bool SelectShapeById(int id)
    {
        var shape = Globals.ThisAddIn.Application.ActivePage.Shapes.OfType<Shape>().SingleOrDefault(x => x.ID == id);
        if (shape == null) return false;

        // select and center screen
        Globals.ThisAddIn.Application.ActivePage.Application.ActiveWindow.Select(shape, (short)VisSelectArgs.visSelect);
        Globals.ThisAddIn.Application.ActivePage.Application.ActiveWindow.CenterViewOnShape(shape,
            VisCenterViewFlags.visCenterViewSelectShape);
        return true;
    }
}