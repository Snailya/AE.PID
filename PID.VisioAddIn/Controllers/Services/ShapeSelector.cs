using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using AE.PID.ViewModels;
using AE.PID.Views;
using Microsoft.Office.Interop.Visio;
using NLog;

namespace AE.PID.Controllers.Services;

/// <summary>
/// A selection service used for enhance user selection.
/// </summary>
public static class ShapeSelector
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static Subject<IVPage> ManuallyInvokeTrigger { get; } = new();

    /// <summary>
    ///     Emit a value manually
    /// </summary>
    /// <param name="page"></param>
    public static void Invoke(IVPage page)
    {
        ManuallyInvokeTrigger.OnNext(page);
    }

    /// <summary>
    /// Start listening for select button click event and display a view to accept user operation.
    /// The view prompt user to switch between select by id and by type, the subsequent is called in ViewModel.
    /// </summary>
    /// <returns></returns>
    public static IDisposable Listen()
    {
        Logger.Info($"Select Service started.");

        return ManuallyInvokeTrigger
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(Globals.ThisAddIn.SynchronizationContext)
            .Select(_ =>
            {
                Globals.ThisAddIn.MainWindow.Content = new ShapeSelectionView();
                Globals.ThisAddIn.MainWindow.Show();

                return Unit.Default;
            })
            .Subscribe(
                _ => { },
                ex =>
                {
                    ThisAddIn.Alert(ex.Message);
                    Logger.Error(ex,
                        $"Select Service ternimated accidently.");
                },
                () => { Logger.Error("Select Service should never complete."); }
            );
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