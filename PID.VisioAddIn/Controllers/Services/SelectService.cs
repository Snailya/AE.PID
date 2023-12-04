using System;
using System.Collections.Generic;
using System.Linq;
using AE.PID.Models;
using AE.PID.Views;
using Microsoft.Office.Interop.Visio;
using NLog;
using Window = System.Windows.Window;

namespace AE.PID.Controllers.Services;

public class SelectService
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private Window _shapeSelectPromptWindow;

    /// <summary>
    ///     Get masters in document stencil.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<VisMaster> GetMastersSource()
    {
        return Globals.ThisAddIn.Application.ActiveDocument.Masters
            .OfType<IVMaster>().Select(x => new VisMaster { BaseID = x.BaseID, Name = x.Name });
    }

    /// <summary>
    ///     Display a view to let user select select mode.
    /// </summary>
    public void DisplayView()
    {
        if (_shapeSelectPromptWindow == null)
        {
            _shapeSelectPromptWindow = new Window
            {
                Title = "选择",
                Height = 480,
                Width = 320,
                MinHeight = 480,
                MinWidth = 320,
                Content = new ShapeSelectionView()
            };

            _shapeSelectPromptWindow.Closed += ShapeSelectPromptWindow_Closed;
            _shapeSelectPromptWindow.Show();
        }

        _shapeSelectPromptWindow.Activate();
    }

    private void ShapeSelectPromptWindow_Closed(object sender, EventArgs e)
    {
        _shapeSelectPromptWindow = null;
    }

    /// <summary>
    ///     Close window called by command.
    /// </summary>
    public void CloseShapeSelectPromptWindow()
    {
        if (_shapeSelectPromptWindow == null) return;

        _shapeSelectPromptWindow.Close();
        _shapeSelectPromptWindow = null;
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
    public static void SelectShapeById(int id)
    {
        Globals.ThisAddIn.Application.ActiveWindow.Select(
            Globals.ThisAddIn.Application.ActivePage.Shapes.ItemFromID[id], (short)VisSelectArgs.visSelect);
    }
}