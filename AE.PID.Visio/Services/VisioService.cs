using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using AE.PID.Core.Models;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using AE.PID.Visio.Extensions;
using AE.PID.Visio.Helpers;
using AE.PID.Visio.Shared.Services;
using DynamicData;
using Microsoft.Office.Interop.Excel;
using Microsoft.Office.Interop.Visio;
using Splat;
using Shape = Microsoft.Office.Interop.Visio.Shape;

namespace AE.PID.Visio.Services;

public class VisioService : DisposableBase, IVisioService
{
    private readonly Document _document;

    private readonly SourceCache<FunctionLocation, CompositeId> _functionLocations = new(t => t.Id);
    private readonly SourceCache<MaterialLocation, CompositeId> _materialLocations = new(t => t.LocationId);
    private readonly SourceCache<Symbol, string> _symbols = new(t => t.Id);

    public VisioService(Document document, IScheduler? scheduler = null)
    {
        scheduler ??= Scheduler.CurrentThread;

        _document = document;

        var shapes = new Lazy<IObservable<IChangeSet<Shape, CompositeId>>>(() =>
        {
            this.Log().Debug(
                $"{DateTime.Now}Convert shapes to ObservableChangeSet on thread: {Thread.CurrentThread.ManagedThreadId}");

            return Observable.Start(document.ToShapeChangeSet, scheduler)
                .Switch()
                .RefCount()
#if DEBUG
                .OnItemAdded(x => this.Log().Debug($"Detect Shape Added: {x}"))
                .OnItemRemoved(x => this.Log().Debug($"Detect Shape Removed: {x}"))
                .OnItemRefreshed(x => this.Log().Debug($"Detect Shape Refreshed: {x}"))
                .OnItemUpdated((current, previous) => this.Log().Debug($"Detect Shape Updated: {current}"))
#endif
                .Transform(GetShape);
        });

        FunctionLocations = new Lazy<IObservableCache<FunctionLocation, CompositeId>>(() =>
        {
            this.Log().Debug(
                $"Convert function locations to ObservableChangeSet on thread: {Thread.CurrentThread.ManagedThreadId}");

            Observable.Start(() =>
            {
                shapes.Value
                    .Filter(x => x.IsFunctionLocation())
                    .Transform(x => x.ToFunctionLocation())
                    .ObserveOn(TaskPoolScheduler.Default)
#if DEBUG
                    .OnItemAdded(x => this.Log().Debug($"Detect FunctionLocation Added: {x.Id}"))
                    .OnItemRemoved(x => this.Log().Debug($"Detect FunctionLocation Removed: {x.Id}"))
                    .OnItemRefreshed(x => this.Log().Debug($"Detect FunctionLocation Refreshed: {x.Id}"))
                    .OnItemUpdated(
                        (current, previous) => this.Log().Debug($"Detect FunctionLocation Updated: {current.Id}"))
#endif
                    .PopulateInto(_functionLocations)
                    .DisposeWith(CleanUp);
            }, scheduler);

            return _functionLocations.AsObservableCache();
        });

        MaterialLocations = new Lazy<IObservableCache<MaterialLocation, CompositeId>>(() =>
        {
            this.Log().Debug(
                $"Convert material locations to ObservableChangeSet on thread: {Thread.CurrentThread.ManagedThreadId}");

            Observable.Start(() =>
            {
                shapes.Value
                    .Filter(x => x.IsMaterialLocation())
                    .Transform(x => x.ToMaterialLocation())
                    .ObserveOn(TaskPoolScheduler.Default)
#if DEBUG
                    .OnItemAdded(x => this.Log().Debug($"Detect MaterialLocation Added: {x.LocationId}"))
                    .OnItemRemoved(x => this.Log().Debug($"Detect MaterialLocation Removed: {x.LocationId}"))
                    .OnItemRefreshed(x => this.Log().Debug($"Detect MaterialLocation Refreshed: {x.LocationId}"))
                    .OnItemUpdated((current, previous) =>
                        this.Log().Debug($"Detect MaterialLocation Updated: {current.LocationId}"))
#endif
                    .PopulateInto(_materialLocations)
                    .DisposeWith(CleanUp);
            }, scheduler);

            return _materialLocations.AsObservableCache();
        });

        Symbols = new Lazy<IObservableCache<Symbol, string>>(() =>
        {
            scheduler.Schedule(() =>
            {
                this.Log().Debug(
                    $"{DateTime.Now}Convert masters locations to ObservableChangeSet on thread: {Thread.CurrentThread.ManagedThreadId}");

                document.ToMasterChangeSet()
#if DEBUG
                    .OnItemAdded(x => this.Log().Debug($"Detect Master Added: {x}"))
                    .OnItemRemoved(x => this.Log().Debug($"Detect Master Removed: {x}"))
                    .OnItemRefreshed(x => this.Log().Debug($"Detect Master Refreshed: {x}"))
                    .OnItemUpdated((current, previous) =>
                        this.Log().Debug($"Detect Master Updated: {current}"))
#endif
                    .Transform(GetMaster)
                    .Transform(x => x.ToSymbol())
                    .PopulateInto(_symbols)
                    .DisposeWith(CleanUp);
            });

            return _symbols.AsObservableCache();
        });
    }

    public Lazy<IObservableCache<FunctionLocation, CompositeId>> FunctionLocations { get; }
    public Lazy<IObservableCache<MaterialLocation, CompositeId>> MaterialLocations { get; }

    public Lazy<IObservableCache<Symbol, string>> Symbols { get; }

    public void SelectAndCenterView(CompositeId id)
    {
        var shape = _document.Pages.ItemFromID[id.PageId].Shapes.ItemFromID[id.ShapeId];
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
        UpdateProperties(_document.DocumentSheet, patches);
    }

    public void UpdatePageProperties(int id, IEnumerable<ValuePatch> patches)
    {
        var page = _document.Pages.ItemFromID[id];
        UpdateProperties(page.PageSheet, patches);
    }

    public void UpdateShapeProperties(CompositeId id, IEnumerable<ValuePatch> patches)
    {
        var shape = GetShape(id);
        UpdateProperties(shape, patches);
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

    // public void PersistAsSolutionXml<TObject, TKey>(string keyword, TObject[] items,
    //     Func<TObject, TKey> keySelector, bool overwrite = false)
    //     where TKey : notnull
    // {
    //     List<TObject>? solutionItems;
    //
    //     if (!overwrite)
    //         try
    //         {
    //             // replace the origin project xml
    //             solutionItems = ReadFromSolutionXml<List<TObject>>(keyword);
    //
    //             foreach (var item in items)
    //                 solutionItems.ReplaceOrAdd(
    //                     solutionItems.SingleOrDefault(x => Equals(keySelector(x), keySelector(item))), item);
    //         }
    //         catch (FileNotFoundException e)
    //         {
    //             // or create a new one
    //             solutionItems = items.ToList();
    //         }
    //     else
    //         solutionItems = items.ToList();
    //
    //
    //     // persist
    //     var element = new SolutionXmlElement<List<TObject>>
    //     {
    //         Name = keyword,
    //         Data = solutionItems
    //     };
    //     SolutionXmlHelper.Store(_document, element);
    //
    //     this.Log().Info($"{items.Length} items saved with keyword {keyword} as solution xml.");
    // }


    public string? GetDocumentProperty<T>(string propName)
    {
        return _document.DocumentSheet.TryGetValue(propName);
    }

    private static void UpdateProperties(Shape shape, IEnumerable<ValuePatch> patches)
    {
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

    public static void SelectAndCenterView(int id)
    {
        var shape = Globals.ThisAddIn.Application.ActivePage.Shapes.OfType<Shape>().SingleOrDefault(x => x.ID == id);
        if (shape == null) throw new ArgumentOutOfRangeException(nameof(CompositeId));

        Globals.ThisAddIn.Application.ActivePage.Application.ActiveWindow.Select(shape, (short)VisSelectArgs.visSelect);
        Globals.ThisAddIn.Application.ActivePage.Application.ActiveWindow.CenterViewOnShape(shape,
            VisCenterViewFlags.visCenterViewSelectShape);
    }

    public static void SelectAndCenterView(string[] ids)
    {
        var shapeIds = new List<int>();
        foreach (var id in ids)
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