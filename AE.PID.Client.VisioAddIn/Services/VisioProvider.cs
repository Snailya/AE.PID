using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using AE.PID.Client.Core;
using AE.PID.Client.Core.VisioExt;
using AE.PID.Client.Core.VisioExt.Models;
using AE.PID.Client.Infrastructure;
using AE.PID.Client.Infrastructure.Extensions;
using AE.PID.Core.Models;
using DynamicData;
using Microsoft.Office.Interop.Excel;
using Microsoft.Office.Interop.Visio;
using Page = Microsoft.Office.Interop.Visio.Page;
using Shape = Microsoft.Office.Interop.Visio.Shape;

namespace AE.PID.Client.VisioAddIn;

public class VisioProvider : DisposableBase, IVisioDataProvider
{
    private readonly Document _document;
    private readonly Lazy<IDisposable> _loader;
    private readonly IScheduler _scheduler;

    private readonly SourceCache<VisioShape, VisioShapeId> _shapes = new(x => x.Id);

    #region -- Constructors --

    public VisioProvider(Document document, IScheduler? scheduler = null)
    {
        _document = document;
        _scheduler = scheduler ?? Scheduler.CurrentThread;

        _loader = new Lazy<IDisposable>(() =>
        {
            // ensure the to change set method invoke on the target scheduler
            return document.ToShapeChangeSet()
                .SubscribeOn(_scheduler)
#if DEBUG
                .OnItemAdded(x => DebugExt.Log("Shape Added", x.Id, nameof(VisioProvider)))
                .OnItemUpdated((cur, prev, _) => DebugExt.Log("Shape Updated", cur.Id, nameof(VisioProvider)))
                .OnItemRefreshed(x => DebugExt.Log("Shape Refreshed", x.Id, nameof(VisioProvider)))
                .OnItemRemoved(x => DebugExt.Log("Shape Removed", x.Id, nameof(VisioProvider)))
#endif
                .PopulateInto(_shapes);
        });

        var current = new ProjectLocation(new VisioDocumentId(_document.ID),
            _document.DocumentSheet.TryGetValue<int>(CellNameDict.ProjectId));
        ProjectLocation = Observable.FromEvent<EShape_CellChangedEventHandler, Cell>(
                handler => _document.DocumentSheet.CellChanged += handler,
                handler => _document.DocumentSheet.CellChanged -= handler)
            .Where(x => x.LocalName == CellNameDict.ProjectId)
            .Select(x => new ProjectLocation(new VisioDocumentId(_document.ID),
                _document.DocumentSheet.TryGetValue<int>(CellNameDict.ProjectId)))
            .StartWith(current);

        // convert the shapes to function locations and material locations. 
        // as the _shapes are lazy loaded and controlled by an external call, this transformation will not bring delay time when initialized, so that the UI will not bolck
        FunctionLocations = _shapes
            .Connect()
            .Filter(x => x.Types.Contains(LocationType.FunctionLocation))
            .Transform(ToFunctionLocation)
            // after the shape is transformed into the model, switch the forward working into background scheduler
            .ChangeKey(x => x.Id)
            .ObserveOn(TaskPoolScheduler.Default)
            .AsObservableCache();

        MaterialLocations = _shapes
            .Connect()
            .Filter(x => x.Types.Contains(LocationType.MaterialLocation))
            .Transform(ToMaterialLocation)
            // after the shape is transformed into the model, switch the forward working into background scheduler
            .ChangeKey(x => x.Id)
            .ObserveOn(TaskPoolScheduler.Default)
            .AsObservableCache();

        // because the masters is always in small amount, no need to control the load behavior externally
        Masters = new Lazy<IObservableCache<VisioMaster, string>>(() => document.ToMasterChangeSet()
            .SubscribeOn(_scheduler)
            .AsObservableCache());

        ProjectLocationUpdater.Subscribe(location =>
            {
                UpdateProperties([
                    new PropertyPatch(location.Id, CellNameDict.ProjectId, location.ProjectId ?? 0, true)
                ]);
            })
            .DisposeWith(CleanUp);

        MaterialLocationsUpdater
            .SelectMany(x => x)
            .Select(BuildPropertyPatch)
            .Subscribe(UpdateProperties)
            .DisposeWith(CleanUp);

        FunctionLocationsUpdater
            .SelectMany(x => x)
            .Select(BuildPropertyPatch)
            .Subscribe(UpdateProperties)
            .DisposeWith(CleanUp);

        // todo: refactor function location into record
    }

    #endregion

    #region -- IInteractive --

    public void Select(ICompoundKey[] ids)
    {
        var shapeIds = new List<int>();

        foreach (var id in ids)
            switch (id)
            {
                case VisioShapeId shapeId:
                    shapeIds.Add(shapeId.ShapeId);
                    break;
                case VisioMasterId masterId:
                {
                    var master = Globals.ThisAddIn.Application.ActiveDocument.Masters[$"B{masterId.BaseId}"];
                    Globals.ThisAddIn.Application.ActivePage.CreateSelection(VisSelectionTypes.visSelTypeByMaster,
                        VisSelectMode.visSelModeSkipSuper, master).GetIDs(out var shapeIdsPerMaster);
                    shapeIds.AddRange(shapeIdsPerMaster.OfType<int>());
                    break;
                }
            }

        SelectAndCenterView(shapeIds.ToArray());
    }

    #endregion

    #region -- ILazyLoad --

    public void Load()
    {
        _ = _loader.Value;
    }

    #endregion

    #region -- IVisioDataProvider --

    public Lazy<IObservableCache<VisioMaster, string>> Masters { get; set; }

    #endregion

    #region -- IOleSupport --

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

    #endregion

    public string? GetDocumentProperty(string propName)
    {
        return _document.DocumentSheet.TryGetValue(propName);
    }

    public string? GetPageProperty(int id, string propName)
    {
        var page = _document.Pages.ItemFromID[id];
        return page.PageSheet.TryGetValue(propName);
    }

    public string? GetShapeProperty(VisioShapeId id, string propName)
    {
        var shape = GetShape(id);
        return shape.TryGetValue(propName);
    }

    private FunctionLocation ToFunctionLocation(VisioShape shape)
    {
        return _document.Pages.ItemFromID[shape.Id.PageId].Shapes.ItemFromID[shape.Id.ShapeId].ToFunctionLocation();
    }

    private MaterialLocation ToMaterialLocation(VisioShape shape)
    {
        return _document.Pages.ItemFromID[shape.Id.PageId].Shapes.ItemFromID[shape.Id.ShapeId].ToMaterialLocation();
    }

    public override void Dispose()
    {
        base.Dispose();

        if (_loader.IsValueCreated)
            _loader.Value.Dispose();
    }

    private Shape GetShape(VisioShapeId id)
    {
        return _document.Pages.ItemFromID[id.PageId].Shapes.ItemFromID[id.ShapeId];
    }

    private static void SelectAndCenterView(int[] shapeIds)
    {
        var selection = Globals.ThisAddIn.Application.ActivePage.CreateSelection(VisSelectionTypes.visSelTypeEmpty);
        foreach (var id in shapeIds)
            selection.Select(Globals.ThisAddIn.Application.ActivePage.Shapes.ItemFromID[id],
                (short)VisSelectArgs.visSelect);
        Globals.ThisAddIn.Application.ActiveWindow.Selection = selection;
    }

    private static IEnumerable<PropertyPatch> BuildPropertyPatch(MaterialLocation location)
    {
        return
        [
            new PropertyPatch(location.Id, CellNameDict.MaterialCode, location.Code, true),
            new PropertyPatch(location.Id, CellNameDict.UnitQuantity, location.Quantity)
        ];
    }

    private static IEnumerable<PropertyPatch> BuildPropertyPatch(FunctionLocation location)
    {
        var patches = new List<PropertyPatch>();

        switch (location.Type)
        {
            case FunctionType.Equipment or FunctionType.Instrument
                or FunctionType.FunctionElement:
                var value = Regex.Match(location.Element, @"\d+").Value;
                patches.AddRange([
                    new PropertyPatch(location.Id, CellNameDict.FunctionElement, value),
                    new PropertyPatch(location.Id, CellNameDict.Description, location.Description)
                ]);
                break;
            case FunctionType.FunctionGroup:
                patches.AddRange([
                    new PropertyPatch(location.Id, CellNameDict.FunctionGroup, location.Group),
                    new PropertyPatch(location.Id, CellNameDict.FunctionGroupName, location.GroupName),
                    new PropertyPatch(location.Id, CellNameDict.FunctionGroupEnglishName,
                        location.GroupEnglishName),
                    new PropertyPatch(location.Id, CellNameDict.FunctionGroupDescription,
                        location.Description)
                ]);

                break;
            case FunctionType.ProcessZone:
                patches.AddRange([
                    new PropertyPatch(location.Id, CellNameDict.FunctionZone, location.Zone),
                    new PropertyPatch(location.Id, CellNameDict.FunctionZoneName, location.ZoneName),
                    new PropertyPatch(location.Id, CellNameDict.FunctionZoneEnglishName,
                        location.ZoneEnglishName)
                ]);

                break;
        }

        if (location.FunctionId != null)
            patches.Add(new PropertyPatch(location.Id, CellNameDict.FunctionId, location.FunctionId, true));
        patches.Add(new PropertyPatch(location.Id, CellNameDict.Remarks, location.Remarks, true, "\"备注\""));

        return patches;
    }

    private void UpdateProperties(IEnumerable<PropertyPatch> properties)
    {
        _scheduler.Schedule(() =>
        {
            foreach (var property in properties)
                switch (property.Target)
                {
                    case VisioMasterId visioMasterId:

                        break;
                    case VisioShapeId visioShapeId:
                        var shape = GetShape(visioShapeId);
                        shape.TrySetValue(property.Name, property.Value, property.CreateIfNotExists,
                            property.LabelFormula);
                        break;
                    case VisioPageId visioPageId:
                        var page = GetPage(visioPageId);
                        page.PageSheet.TrySetValue(property.Name, property.Value, property.CreateIfNotExists,
                            property.LabelFormula);
                        break;
                    case VisioDocumentId visioDocument:
                        if (_document.ID == visioDocument.ComputedId)
                            _document.DocumentSheet.TrySetValue(property.Name, property.Value,
                                property.CreateIfNotExists);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
        });
    }

    private Page GetPage(VisioPageId visioPageId)
    {
        return _document.Pages.ItemFromID[visioPageId.ComputedId];
    }

    #region -- IDataProvider --

    public IObservable<ProjectLocation> ProjectLocation { get; }
    public Subject<ProjectLocation> ProjectLocationUpdater { get; } = new();
    public IObservableCache<FunctionLocation, ICompoundKey> FunctionLocations { get; }
    public IObservableCache<FunctionLocationDetail, ICompoundKey> FunctionLocationDetails { get; }
    public Subject<FunctionLocation[]> FunctionLocationsUpdater { get; } = new();
    public Subject<FunctionLocationDetail[]> FunctionLocationDetailsUpdater { get; }
    public IObservableCache<MaterialLocation, ICompoundKey> MaterialLocations { get; }
    public Subject<MaterialLocation[]> MaterialLocationsUpdater { get; } = new();

    public ICompoundKey[] GetAdjacent(ICompoundKey compositeId)
    {
        if (compositeId is VisioShapeId visioShapeId)
            return _document.Pages.ItemFromID[visioShapeId.PageId].Shapes.ItemFromID[visioShapeId.ShapeId]
                .ConnectedShapes(VisConnectedShapesFlags.visConnectedShapesAllNodes, "").OfType<int>()
                .Select(x => new VisioShapeId(visioShapeId.PageId, x)).OfType<ICompoundKey>().ToArray();

        throw new ArgumentException();
    }

    #endregion
}