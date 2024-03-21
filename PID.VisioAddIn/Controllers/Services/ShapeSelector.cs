using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using AE.PID.Models;
using AE.PID.Models.BOM;
using AE.PID.Models.VisProps;
using AE.PID.ViewModels;
using AE.PID.Views;
using AE.PID.Views.Controls;
using AE.PID.Views.Pages;
using DynamicData;
using Microsoft.Office.Interop.Visio;
using NLog;

namespace AE.PID.Controllers.Services;

/// <summary>
/// A selection service used for enhance user selection.
/// </summary>
public class ShapeSelector
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static Subject<IVDocument> ManuallyInvokeTrigger { get; } = new();

    private readonly SourceCache<IVMaster, int> _masters = new(t => t.ID);
    private readonly Document _document;

    public ShapeSelector(Document document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));

        // initialize items by get all items from current page
        var masters = GetMastersFromDocument(_document);
        if (masters != null)
            _masters.AddOrUpdate(masters);
    }

    /// <summary>
    ///     Emit a value manually
    /// </summary>
    /// <param name="document"></param>
    public static void Invoke(IVDocument document)
    {
        ManuallyInvokeTrigger.OnNext(document);
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
            .Subscribe(
                _ =>
                {
                    try
                    {
                        Globals.ThisAddIn.WindowManager.Show(new ShapeSelectionPage());
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"Failed to display selection window.");
                        ThisAddIn.Alert($"加载失败：{ex.Message}");
                    }
                },
                ex => { Logger.Error(ex, $"Select Service ternimated accidently."); },
                () => { Logger.Error("Select Service should never complete."); }
            );
    }

    public IObservableCache<IVMaster, int> Masters => _masters.AsObservableCache();

    /// <summary>
    /// Listen on the document master change
    /// </summary>
    /// <returns>A <see cref="Disposable"/> to unsubscribe from these change events</returns>
    public CompositeDisposable MonitorChange()
    {
        var subscription = new CompositeDisposable();

        // when a shape's property is modified, it will raise up FormulaChanged event, so that the modification could be captured to emit as a new value
        var observeChanged = Observable
            .FromEvent<EDocument_MasterAddedEventHandler, Master>(
                handler => _document.MasterAdded += handler,
                handler => _document.MasterAdded -= handler)
            .Do(master =>
            {
                if (master != null) _masters.AddOrUpdate(master);
            })
            .Subscribe()
            .DisposeWith(subscription);

        // when a new shape is add to the page, it could be captured using ShapeAdded event
        var observeDeleted = Observable.FromEvent<EDocument_BeforeMasterDeleteEventHandler, Master>(
                handler => _document.BeforeMasterDelete += handler,
                handler => _document.BeforeMasterDelete -= handler)
            .Do(master => { _masters.Remove(master); })
            .Subscribe()
            .DisposeWith(subscription);

        // return a disposable to unsubscribe from all these change event
        return subscription;
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
        var shape = Globals.ThisAddIn.Application.ActivePage.Shapes.OfType<Shape>().SingleOrDefault(x => x.ID == id);
        if (shape != null)
            Globals.ThisAddIn.Application.ActiveWindow.Select(shape, (short)VisSelectArgs.visSelect);
    }

    /// <summary>
    /// Get all masters from document stencil.
    /// </summary>
    /// <returns>A collection of elements from page</returns>
    private static IEnumerable<IVMaster> GetMastersFromDocument(IVDocument document)
    {
        return document.Masters
            .OfType<IVMaster>()
            .Where(x => x is not null);
    }
}