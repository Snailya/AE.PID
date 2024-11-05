using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using AE.PID.Visio.Core.Exceptions;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using AE.PID.Visio.Extensions;
using AE.PID.Visio.Shared.Extensions;
using AE.PID.Visio.Shared.Services;
using DynamicData;
using Microsoft.Office.Interop.Excel;
using Microsoft.Office.Interop.Visio;
using Shape = Microsoft.Office.Interop.Visio.Shape;

namespace AE.PID.Visio.Services;

public class VisioService : DisposableBase, IVisioService
{
    private readonly Document _document;
    private readonly IScheduler _scheduler;

    public VisioService(Document document, IScheduler? scheduler = null)
    {
        _document = document;
        _scheduler = scheduler ?? Scheduler.CurrentThread;

        Shapes = new Lazy<IObservableCache<VisioShape, CompositeId>>(() =>
        {
            // ensure the to change set method invoke on the target scheduler
            return document.ToShapeChangeSet()
                .SubscribeOn(_scheduler)
#if DEBUG
                .OnItemAdded(x => DebugExt.Log("Shape Added", x.Id, nameof(VisioService)))
                .OnItemUpdated((cur, prev, _) => DebugExt.Log("Shape Updated", cur.Id, nameof(VisioService)))
                .OnItemRefreshed(x => DebugExt.Log("Shape Refreshed", x.Id, nameof(VisioService)))
                .OnItemRemoved(x => DebugExt.Log("Shape Removed", x.Id, nameof(VisioService)))
#endif
                .AsObservableCache();
        });

        Masters = new Lazy<IObservableCache<VisioMaster, string>>(() => document.ToMasterChangeSet()
            .SubscribeOn(_scheduler)
            .AsObservableCache());
    }

    public Lazy<IObservableCache<VisioMaster, string>> Masters { get; }
    public Lazy<IObservableCache<VisioShape, CompositeId>> Shapes { get; }

    public CompositeId[] GetAdjacent(CompositeId compositeId)
    {
        return _document.Pages.ItemFromID[compositeId.PageId].Shapes.ItemFromID[compositeId.ShapeId]
            .ConnectedShapes(VisConnectedShapesFlags.visConnectedShapesAllNodes, "").OfType<int>()
            .Select(x => new CompositeId(compositeId.PageId, x)).ToArray();
    }

    public FunctionLocation ToFunctionLocation(VisioShape shape)
    {
        return _document.Pages.ItemFromID[shape.Id.PageId].Shapes.ItemFromID[shape.Id.ShapeId].ToFunctionLocation();
    }

    public MaterialLocation ToMaterialLocation(VisioShape shape)
    {
        return _document.Pages.ItemFromID[shape.Id.PageId].Shapes.ItemFromID[shape.Id.ShapeId].ToMaterialLocation();
    }

    public void SelectAndCenterView(CompositeId id)
    {
        var shape = _document.Pages.ItemFromID[id.PageId].Shapes.ItemFromID[id.ShapeId];
        if (shape == null) throw new ShapeNotExistException(id);

        _document.Application.ActivePage.Application.ActiveWindow.Select(shape, (short)VisSelectArgs.visSelect);
        _document.Application.ActivePage.Application.ActiveWindow.CenterViewOnShape(shape,
            VisCenterViewFlags.visCenterViewSelectShape);
    }


    public string? GetDocumentProperty(string propName)
    {
        return _document.DocumentSheet.TryGetValue(propName);
    }

    public string? GetPageProperty(int id, string propName)
    {
        var page = _document.Pages.ItemFromID[id];
        return page.PageSheet.TryGetValue(propName);
    }

    public string? GetShapeProperty(CompositeId id, string propName)
    {
        var shape = GetShape(id);
        return shape.TryGetValue(propName);
    }

    public void UpdateDocumentProperties(IEnumerable<ValuePatch> patches)
    {
        // update the properties in the specified scheduler
        _scheduler.Schedule(() => { UpdatePropertiesImpl(_document.DocumentSheet, patches); });
    }

    public void UpdatePageProperties(int id, IEnumerable<ValuePatch> patches)
    {
        // update the properties in the specified scheduler
        _scheduler.Schedule(() =>
        {
            var page = _document.Pages.ItemFromID[id];
            UpdatePropertiesImpl(page.PageSheet, patches);
        });
    }

    public void UpdateShapeProperties(CompositeId id, IEnumerable<ValuePatch> patches)
    {
        // update the properties in the specified scheduler
        _scheduler.Schedule(() =>
        {
            var shape = GetShape(id);
            UpdatePropertiesImpl(shape, patches);
        });
    }

    public void InsertAsExcelSheet(string[,] dataArray)
    {
        var oleShape = Globals.ThisAddIn.Application.ActivePage.InsertObject("Excel.Sheet",
            (short)VisInsertObjArgs.visInsertAsEmbed);
        object oleObject = oleShape.Object;
        var workbook = (Workbook)oleObject;

        // 操作Excel对象
        var worksheet = (Worksheet)workbook.Worksheets[1];
        worksheet.Range["A1"].Resize[dataArray.GetLength(0), dataArray.GetLength(1)].Value = dataArray;
        worksheet.Columns.AutoFit();
        // 保存并关闭Excel工作簿
        workbook.Save();
        workbook.Close(false); // 关闭工作簿，但不保存改变
        Marshal.ReleaseComObject(worksheet);
        Marshal.ReleaseComObject(workbook);
    }

    public override void Dispose()
    {
        base.Dispose();

        if (Shapes.IsValueCreated)
            Shapes.Value.Dispose();
    }

    public string? GetDocumentProperty<T>(string propName)
    {
        return _document.DocumentSheet.TryGetValue(propName);
    }

    private static void UpdatePropertiesImpl(Shape shape, IEnumerable<ValuePatch> patches)
    {
#if DEBUG
        DebugExt.Log("Update ShapeSheet", null, nameof(VisioService));
#endif
        // XXX: unable to refactor using Shape.SetResults method because it is impossible to know the index of custom cells unless you get it first.
        foreach (var patch in patches) shape.TrySetValue(patch.PropertyName, patch.Value, patch.CreateIfNotExists);
    }

    private Shape GetShape(CompositeId id)
    {
        return _document.Pages.ItemFromID[id.PageId].Shapes.ItemFromID[id.ShapeId];
    }

    private Master GetMaster(string baseId)
    {
        return _document.Masters[$"B{baseId}"];
    }


    public static void SelectAndCenterView(string[] baseIds)
    {
        var shapeIds = new List<int>();
        foreach (var id in baseIds)
        {
            var master = Globals.ThisAddIn.Application.ActiveDocument.Masters[$"B{id}"];
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
}