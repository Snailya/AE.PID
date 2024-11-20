using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Core.Models;
using AE.PID.Visio.Core.Models;
using AE.PID.Visio.Shared;
using AE.PID.Visio.Shared.Extensions;
using DynamicData;
using Microsoft.Office.Interop.Visio;
using Splat;

namespace AE.PID.Visio.Extensions;

public static class ChangeSetExt
{
    private static readonly string[] CellValuesToMonitor =
    {
        CellNameDict.FunctionZone, CellNameDict.FunctionZoneName, CellNameDict.FunctionZoneEnglishName,
        CellNameDict.FunctionGroup, CellNameDict.FunctionGroupName, CellNameDict.FunctionZoneEnglishName,
        CellNameDict.FunctionGroupDescription,
        CellNameDict.FunctionElement, CellNameDict.ElementName, CellNameDict.Description,
        CellNameDict.Remarks,
        CellNameDict.SubClass, CellNameDict.KeyParameters, CellNameDict.UnitQuantity, CellNameDict.Quantity,
        CellNameDict.MaterialCode,
        CellNameDict.Customer
    };

    /// <summary>
    ///     Create a change set that monitors maters add and removed event of the document.
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    public static IObservable<IChangeSet<VisioMaster, string>> ToMasterChangeSet(
        this Document document)
    {
        return ObservableChangeSet.Create<VisioMaster, string>(cache =>
        {
            var subscription = new CompositeDisposable();

            var observeAdded = Observable.FromEvent<EDocument_MasterAddedEventHandler, Master>(
                    handler => document.MasterAdded += handler,
                    handler => document.MasterAdded -= handler,
                    SchedulerManager.VisioScheduler)
                .Select(master => new VisioMaster(master.BaseID, master.Name))
#if DEBUG
                .Do(x => DebugExt.Log("MasterAdded", x))
#endif
                .Do(cache.AddOrUpdate);

            var observeRemoved = Observable.FromEvent<EDocument_BeforeMasterDeleteEventHandler, Master>(
                    handler => document.BeforeMasterDelete += handler,
                    handler => document.BeforeMasterDelete -= handler,
                    SchedulerManager.VisioScheduler)
                .Select(master => new VisioMaster(master.BaseID, master.Name))
#if DEBUG
                .Do(x => DebugExt.Log("BeforeMasterDelete", x))
#endif
                .Do(master => cache.RemoveKey(master.BaseId));

            observeAdded.Merge(observeRemoved)
                .Subscribe()
                .DiffWith(subscription);

            // load initial values
            // todo：最理想的情况是在需要的时候才加载
            var initials = document.Masters.OfType<IVMaster>()
                .Select(master => new VisioMaster(master.BaseID, master.Name))
                .ToList();
            cache.AddOrUpdate(initials);

            return subscription;
        }, t => t.BaseId);
    }

    /// <summary>
    ///     Create a change set that monitors shape add, update and removed event of the document.
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    public static IObservable<IChangeSet<VisioShape, CompositeId>> ToShapeChangeSet(
        this Document document)
    {
        return ObservableChangeSet.Create<VisioShape, CompositeId>(cache =>
            {
                var subscription = new CompositeDisposable();

                // observe each page's change
                document.Pages.OfType<Page>().ToObservable()
                    .Merge(Observable
                        .FromEvent<EDocument_PageAddedEventHandler, Page>(
                            handler => document.PageAdded += handler,
                            handler => document.PageAdded -= handler,
                            SchedulerManager.VisioScheduler))
#if DEBUG
                    .Do(x => DebugExt.Log("PageAdded", x))
#endif
                    .Subscribe(page =>
                    {
                        var observePageChange = page.ToChangeSet()
#if DEBUG
                            .OnItemAdded(x => DebugExt.Log("OnItemAdded", x))
                            .OnItemRemoved(x => DebugExt.Log("OnItemRemoved", x))
                            .OnItemRefreshed(x => DebugExt.Log("OnItemRefreshed", x))
                            .OnItemUpdated((current, previous) =>
                                DebugExt.Log("OnItemUpdated", current))
#endif
                            .PopulateInto(cache)
                            .DisposeWith(subscription);

                        Observable.FromEvent<EPage_BeforePageDeleteEventHandler, Page>(
                                handler => page.BeforePageDelete += handler,
                                handler => page.BeforePageDelete -= handler,
                                SchedulerManager.VisioScheduler)
#if DEBUG
                            .Do(x => DebugExt.Log("BeforePageDelete", x))
#endif
                            .Subscribe(x =>
                            {
                                var items = cache.Items.Where(i => i.Id.PageId == x.ID).Select(x => x.Id).ToList();
                                LogHost.Default.Debug(
                                    $"Page deleted, going to remove {items.Count} items from changeset.");
                                cache.RemoveKeys(items); // remove items
                                observePageChange.Dispose(); // Dispose observe the shape change on that page
                            })
                            .DisposeWith(subscription);
                    })
                    .DisposeWith(subscription);
                // Return the Disposable that controls the subscription
                return subscription;
            },
            f => f.Id);
    }

    private static FunctionType GetFunctionType(IVShape source)
    {
        return source.HasCategory("Frame") ? FunctionType.ProcessZone :
            source.HasCategory("FunctionalGroup") ? FunctionType.FunctionGroup :
            source.HasCategory("Unit") ? FunctionType.FunctionUnit :
            source.HasCategory("Equipment") ? FunctionType.Equipment :
            source.HasCategory("Instrument") ? FunctionType.Instrument :
            source.HasCategory("FunctionalElement") ? FunctionType.FunctionElement :
            source.HasCategory("Exclude") ? FunctionType.External :
            throw new ArgumentException();
    }


    /// <summary>
    ///     Convert a <see cref="IVShape" /> to <see cref="FunctionLocation" />.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static FunctionLocation ToFunctionLocation(this IVShape source)
    {
        var id = new CompositeId(source.ContainingPageID, source.ID);

        var type = GetFunctionType(source);

        var functionIdStr = source.TryGetValue("User.FunctionId");
        var functionId = double.TryParse(functionIdStr, out var functionIdDouble) ? (int)functionIdDouble : 0;

        var parent = source.MemberOfContainers.OfType<int>().Select(x =>
                new { Id = x, ContainerCount = source.ContainingPage.Shapes.ItemFromID[x].MemberOfContainers.Length })
            .OrderByDescending(x => x.ContainerCount)
            .FirstOrDefault();
        var parentId = new CompositeId(source.ContainingPageID, parent?.Id ?? 0);

        var zone = source.TryGetFormatValue(CellNameDict.FunctionZone) ?? string.Empty;
        var zoneName = source.TryGetFormatValue(CellNameDict.FunctionZoneName) ?? string.Empty;
        var zoneEnglishName = source.TryGetFormatValue(CellNameDict.FunctionZoneEnglishName) ?? string.Empty;

        var group = source.TryGetFormatValue(CellNameDict.FunctionGroup) ?? string.Empty;
        var groupName = source.TryGetFormatValue(CellNameDict.FunctionGroupName) ?? string.Empty;
        var groupEnglishName = source.TryGetFormatValue(CellNameDict.FunctionGroupEnglishName) ?? string.Empty;

        var element = type switch
        {
            FunctionType.ProcessZone => string.Empty,
            FunctionType.FunctionGroup => string.Empty,
            FunctionType.FunctionUnit => string.Empty,
            FunctionType.Equipment => source.TryGetFormatValue(CellNameDict.FunctionElement),
            FunctionType.Instrument => source.TryGetFormatValue(CellNameDict.FunctionElement),
            FunctionType.FunctionElement => source.TryGetValue("Prop.RefEquipment") + "-" +
                                            source.TryGetFormatValue(CellNameDict.FunctionElement),
            FunctionType.External => string.Empty,
            _ => throw new ArgumentOutOfRangeException()
        } ?? string.Empty;

        var name = type switch
        {
            FunctionType.ProcessZone => zoneName,
            FunctionType.FunctionGroup => groupName,
            FunctionType.FunctionUnit => string.Empty,
            FunctionType.Equipment => source.TryGetValue(CellNameDict.SubClass),
            FunctionType.Instrument => source.TryGetValue(CellNameDict.SubClass),
            FunctionType.FunctionElement => source.TryGetValue(CellNameDict.ElementName),
            FunctionType.External => string.Empty,
            _ => throw new ArgumentOutOfRangeException()
        } ?? string.Empty;

        var remarks = source.TryGetValue(CellNameDict.Remarks) ?? string.Empty;

        var description = type switch
        {
            FunctionType.FunctionGroup => source.TryGetValue(CellNameDict.FunctionGroupDescription),
            FunctionType.FunctionUnit => source.TryGetValue(CellNameDict.FunctionGroupDescription),
            FunctionType.Equipment => source.TryGetValue(CellNameDict.Description),
            FunctionType.Instrument => source.TryGetValue(CellNameDict.Description),
            FunctionType.FunctionElement => source.TryGetValue(CellNameDict.Description),
            _ => string.Empty
        } ?? string.Empty;

        var responsibility = type == FunctionType.External
            ? source.TryGetValue(CellNameDict.Customer) ?? string.Empty
            : string.Empty;

        return new FunctionLocation(id, type)
        {
            FunctionId = functionId,
            ParentId = parentId,
            Zone = zone,
            ZoneName = zoneName,
            ZoneEnglishName = zoneEnglishName,
            Group = group,
            GroupName = groupName,
            GroupEnglishName = groupEnglishName,
            Element = element,
            Name = name,
            Remarks = remarks,
            Description = description,
            Responsibility = responsibility
        };
    }

    private static IObservable<IChangeSet<VisioShape, CompositeId>> ToChangeSet(
        this Page page, Func<IVShape, bool>? predicate = null)
    {
        predicate ??= x => true;

        return ObservableChangeSet.Create<VisioShape, CompositeId>(cache =>
        {
            var observeAdded = Observable.FromEvent<EPage_ShapeAddedEventHandler, IVShape>(
                    handler => page.ShapeAdded += handler,
                    handler => page.ShapeAdded -= handler,
                    SchedulerManager.VisioScheduler)
                .Where(predicate)
                .Select(shape =>
                    new VisioShape(new CompositeId(shape.ContainingPageID, shape.ID), GetShapeTypes(shape)))
                .Do(x => DebugExt.Log("ShapeAdded", x))
                .Do(cache.AddOrUpdate);

            var observeRemoved = Observable.FromEvent<EPage_BeforeShapeDeleteEventHandler, IVShape>(
                    handler => page.BeforeShapeDelete += handler,
                    handler => page.BeforeShapeDelete -= handler,
                    SchedulerManager.VisioScheduler)
                .Where(predicate)
                .Select(shape =>
                    new VisioShape(new CompositeId(shape.ContainingPageID, shape.ID), GetShapeTypes(shape)))
                .Do(x => DebugExt.Log("BeforeShapeDelete", x))
                .Do(visioShape => cache.RemoveKey(visioShape.Id));

            var observeCellUpdated = Observable.FromEvent<EPage_CellChangedEventHandler, Cell>(
                    handler => page.CellChanged += handler,
                    handler => page.CellChanged -= handler,
                    SchedulerManager.VisioScheduler)
                .Where(cell => CellValuesToMonitor.Contains(cell.Name))
                .Select(cell => cell.Shape);
            var observeFormulaUpdated = Observable.FromEvent<EPage_FormulaChangedEventHandler, Cell>(
                    handler => page.FormulaChanged += handler,
                    handler => page.FormulaChanged -= handler,
                    SchedulerManager.VisioScheduler)
                .Where(cell => cell.Name == CellNameDict.Relationships)
                .Select(cell => cell.Shape);
            var observeUpdated = observeCellUpdated
                .Merge(observeFormulaUpdated)
                .Where(predicate)
                .Select(shape =>
                    new VisioShape(new CompositeId(shape.ContainingPageID, shape.ID), GetShapeTypes(shape)))
                .QuiescentBuffer(TimeSpan.FromMilliseconds(400), SchedulerManager.VisioScheduler)
                .SelectMany(x => x.Distinct())
                .Do(x => DebugExt.Log("CellChanged", x))
                .Do(cache.AddOrUpdate);

            var subscription = Observable.Merge(observeAdded, observeRemoved, observeUpdated)
                .Subscribe();

            // load initial values
            // todo：最理想的情况是在需要的时候才加载
            var initials = page.Shapes.OfType<IVShape>()
                .Where(predicate)
                .Select(shape =>
                    new VisioShape(new CompositeId(shape.ContainingPageID, shape.ID), GetShapeTypes(shape))).ToList();
            cache.AddOrUpdate(initials);

            return subscription;
        }, t => t.Id);
    }

    /// <summary>
    ///     Convert a <see cref="IVShape" /> to <see cref="MaterialLocation" />.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static MaterialLocation ToMaterialLocation(this IVShape source)
    {
        var locationId = new CompositeId(source.ContainingPageID, source.ID);
        var materialCode = source.CellsU[CellNameDict.MaterialCode]
            .ResultStr[VisUnitCodes.visUnitsString];
        var unitQuantityStr = source.CellsU[CellNameDict.UnitQuantity]
            .ResultStr[VisUnitCodes.visUnitsString];
        var unitQuantity = double.TryParse(unitQuantityStr, out var value) ? value : 0;
        var quantityStr = source.CellsU[CellNameDict.Quantity]
            .ResultStr[VisUnitCodes.visUnitsString];
        var quantity = double.TryParse(quantityStr, out var value2) ? value2 : 0;

        var keyParameters = string.Empty;
        if (source.CellExistsN(CellNameDict.KeyParameters, VisExistsFlags.visExistsAnywhere))
            keyParameters = source.CellsU[CellNameDict.KeyParameters]
                .ResultStr[VisUnitCodes.visUnitsString];

        var type = source.CellsU[CellNameDict.SubClass].ResultStr[VisUnitCodes.visUnitsString];

        return new MaterialLocation(locationId)
        {
            LocationId = locationId,
            Quantity = unitQuantity,
            ComputedQuantity = quantity,
            Code = materialCode ?? string.Empty,
            KeyParameters = keyParameters ?? string.Empty,
            Category = type ?? string.Empty
        };
    }

    private static VisioShape.ShapeType[] GetShapeTypes(this IVShape x)
    {
        var shapeCategories = x.TryGetValue(CellNameDict.ShapeCategories);
        if (shapeCategories == null) return [VisioShape.ShapeType.None];

        return shapeCategories switch
        {
            "Frame" or "FunctionalGroup" or "Unit" or "Exclude" => [VisioShape.ShapeType.FunctionLocation],
            "Equipment" or "Instrument" or "FunctionalElement" =>
            [
                VisioShape.ShapeType.FunctionLocation, VisioShape.ShapeType.MaterialLocation
            ],
            _ => [VisioShape.ShapeType.None]
        };
    }
}