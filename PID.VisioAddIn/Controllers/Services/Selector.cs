using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Interop;
using AE.PID.ViewModels;
using AE.PID.Views;
using Microsoft.Office.Interop.Visio;
using Window = System.Windows.Window;

namespace AE.PID.Controllers.Services;

/// <summary>
/// A selection service used for enhance user selection.
/// </summary>
public static class Selector
{
    private static Window _window;
    static Selector()
    {
        _window = new Window
        {
            Width = 300,
            Height = 300,
            Content = new ShapeSelectionView(),
            Title = "选择工具"
        };
        var unused = new WindowInteropHelper(_window)
        {
            Owner = new IntPtr(Globals.ThisAddIn.Application.WindowHandle32)
        };
        _window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

        _window.Closed += WindowClosed;
    }
    
    /// <summary>
    /// Trigger used for ui Button to invoke the update event.
    /// </summary>
    public static Subject<IVPage> ManuallyInvokeTrigger { get; } = new();

    /// <summary>
    ///     Emit a value manually
    /// </summary>
    /// <param name="page"></param>
    public static void Invoke(IVPage page)
    {
        ManuallyInvokeTrigger.OnNext(page);
    }

    /// <summary>
    ///     Get masters in document stencil.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<MasterViewModel> GetMastersSource()
    {
        return Globals.ThisAddIn.Application.ActivePage.Document.Masters
            .OfType<IVMaster>()
            .Select(x => new MasterViewModel { BaseId = x.BaseID, Name = x.Name, IsChecked = false });
    }

    /// <summary>
    ///     Display a view to let user select select mode.
    /// </summary>
    public static void Display()
    {
        if (_window.Visibility != Visibility.Visible)
            _window.Show();
        _window.Activate();
    }

    private static void WindowClosed(object sender, EventArgs e)
    {
        _window = null;
    }

    /// <summary>
    ///     Close window called by command.
    /// </summary>
    public static void CloseShapeSelectPromptWindow()
    {
        if (_window == null) return;

        _window.Close();
        _window = null;
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