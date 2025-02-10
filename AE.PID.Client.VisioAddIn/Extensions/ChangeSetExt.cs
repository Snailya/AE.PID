using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Client.Core;
using AE.PID.Client.Core.VisioExt.Models;
using AE.PID.Client.Infrastructure.Extensions;
using AE.PID.Client.Infrastructure.VisioExt;
using AE.PID.Core.Models;
using DynamicData;
using Microsoft.Office.Interop.Visio;
using Splat;

namespace AE.PID.Client.VisioAddIn;

public static class ChangeSetExt
{
    private static readonly string[] CellValuesToMonitor =
    {
        CellDict.FunctionZone, CellDict.FunctionZoneName, CellDict.FunctionZoneEnglishName,
        CellDict.FunctionGroup, CellDict.FunctionGroupName, CellDict.FunctionZoneEnglishName,
        CellDict.FunctionGroupDescription,
        CellDict.FunctionElement, CellDict.ElementName, CellDict.Description,
        CellDict.Remarks,
        CellDict.SubClass, CellDict.KeyParameters, CellDict.UnitQuantity, CellDict.Quantity,
        CellDict.MaterialCode,
        CellDict.Customer
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
                .Select(master => new VisioMaster(master.BaseID, master.Name, master.UniqueID))
#if DEBUG
                .Do(x => DebugExt.Log("MasterAdded", x))
#endif
                .Do(cache.AddOrUpdate);

            var observeRemoved = Observable.FromEvent<EDocument_BeforeMasterDeleteEventHandler, Master>(
                    handler => document.BeforeMasterDelete += handler,
                    handler => document.BeforeMasterDelete -= handler,
                    SchedulerManager.VisioScheduler)
                .Select(master => new VisioMaster(master.BaseID, master.Name, master.UniqueID))
#if DEBUG
                .Do(x => DebugExt.Log("BeforeMasterDelete", x))
#endif
                .Do(master => cache.RemoveKey(master.Id.BaseId));

            observeAdded.Merge(observeRemoved)
                .Subscribe()
                .DiffWith(subscription);

            // load initial values
            // todo：最理想的情况是在需要的时候才加载
            var initials = document.Masters.OfType<IVMaster>()
                .Select(master => new VisioMaster(master.BaseID, master.Name, master.UniqueID))
                .ToList();
            cache.AddOrUpdate(initials);

            return subscription;
        }, t => t.Id.BaseId);
    }

    /// <summary>
    ///     Create a change set that monitors shape add, update and removed event of the document.
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    public static IObservable<IChangeSet<VisioShape, VisioShapeId>> ToShapeChangeSet(
        this Document document)
    {
        return ObservableChangeSet.Create<VisioShape, VisioShapeId>(cache =>
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
                            .OnItemUpdated((current, _) =>
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
                                var items = cache.Items.Where(i => i.Id.PageId == x.ID).Select(i => i.Id).ToList();
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
        var shapeCategories = source.TryGetValue(CellDict.ShapeCategories);
        if (shapeCategories == null) throw new ArgumentNullException();

        return shapeCategories switch
        {
            "Frame" => FunctionType.ProcessZone,
            "FunctionalGroup" => FunctionType.FunctionGroup,
            "Unit" => FunctionType.FunctionUnit,
            "Exclude" => FunctionType.External,
            "Equipment" or "Equipments" => FunctionType.Equipment,
            "Instrument" or "Instruments" => FunctionType.Instrument,
            "FunctionalElement" or "FunctionalElements" => FunctionType.FunctionElement,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    /// <summary>
    ///     Convert a <see cref="IVShape" /> to <see cref="FunctionLocation" />.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static FunctionLocation ToFunctionLocation(this IVShape source)
    {
        var id = new VisioShapeId(source.ContainingPageID, source.ID);

        var type = GetFunctionType(source);

        var functionIdStr = source.TryGetValue(CellDict.FunctionId);
        var functionId = double.TryParse(functionIdStr, out var functionIdDouble) ? (int)functionIdDouble : 0;

        var parent = source.MemberOfContainers.OfType<int>().Select(x =>
                new { Id = x, ContainerCount = source.ContainingPage.Shapes.ItemFromID[x].MemberOfContainers.Length })
            .OrderByDescending(x => x.ContainerCount)
            .FirstOrDefault();
        var parentId = new VisioShapeId(source.ContainingPageID, parent?.Id ?? 0);

        var zone = source.TryGetFormatValue(CellDict.FunctionZone) ?? string.Empty;
        var zoneName = source.TryGetFormatValue(CellDict.FunctionZoneName) ?? string.Empty;
        var zoneEnglishName = source.TryGetFormatValue(CellDict.FunctionZoneEnglishName) ?? string.Empty;

        var group = source.TryGetFormatValue(CellDict.FunctionGroup) ?? string.Empty;
        var groupName = source.TryGetFormatValue(CellDict.FunctionGroupName) ?? string.Empty;
        var groupEnglishName = source.TryGetFormatValue(CellDict.FunctionGroupEnglishName) ?? string.Empty;

        var element = type switch
        {
            FunctionType.ProcessZone => string.Empty,
            FunctionType.FunctionGroup => string.Empty,
            FunctionType.FunctionUnit => string.Empty,
            FunctionType.Equipment => source.TryGetFormatValue(CellDict.FunctionElement),
            FunctionType.Instrument => source.TryGetFormatValue(CellDict.FunctionElement),
            FunctionType.FunctionElement => source.TryGetValue(CellDict.RefEquipment) + "-" +
                                            source.TryGetFormatValue(CellDict.FunctionElement),
            FunctionType.External => string.Empty,
            _ => throw new ArgumentOutOfRangeException()
        } ?? string.Empty;

        var name = type switch
        {
            FunctionType.ProcessZone => zoneName,
            FunctionType.FunctionGroup => groupName,
            FunctionType.FunctionUnit => string.Empty,
            FunctionType.Equipment => source.TryGetValue(CellDict.SubClass),
            FunctionType.Instrument => source.TryGetValue(CellDict.SubClass),
            FunctionType.FunctionElement => source.TryGetValue(CellDict.ElementName),
            FunctionType.External => string.Empty,
            _ => throw new ArgumentOutOfRangeException()
        } ?? string.Empty;

        var remarks = source.TryGetValue(CellDict.Remarks) ?? string.Empty;

        var description = type switch
        {
            FunctionType.FunctionGroup => source.TryGetValue(CellDict.FunctionGroupDescription),
            FunctionType.FunctionUnit => source.TryGetValue(CellDict.FunctionGroupDescription),
            FunctionType.Equipment => source.TryGetValue(CellDict.Description),
            FunctionType.Instrument => source.TryGetValue(CellDict.Description),
            FunctionType.FunctionElement => source.TryGetValue(CellDict.Description),
            _ => string.Empty
        } ?? string.Empty;

        var responsibility = type == FunctionType.External
            ? source.TryGetValue(CellDict.Customer) ?? string.Empty
            : string.Empty;

        return new FunctionLocation(id,
            parentId,
            name,
            type,
            functionId,
            zone,
            zoneName,
            zoneEnglishName,
            group,
            groupName,
            groupEnglishName,
            element,
            description,
            remarks,
            responsibility);
    }

    private static IObservable<IChangeSet<VisioShape, VisioShapeId>> ToChangeSet(
        this Page page, Func<IVShape, bool>? predicate = null)
    {
        predicate ??= _ => true;

        return ObservableChangeSet.Create<VisioShape, VisioShapeId>(cache =>
        {
            var observeAdded = Observable.FromEvent<EPage_ShapeAddedEventHandler, IVShape>(
                    handler => page.ShapeAdded += handler,
                    handler => page.ShapeAdded -= handler,
                    SchedulerManager.VisioScheduler)
                .Where(predicate)
                .Select(shape =>
                    new VisioShape(new VisioShapeId(shape.ContainingPageID, shape.ID), GetShapeTypes(shape)))
                .Do(x => DebugExt.Log("ShapeAdded", x))
                .Do(cache.AddOrUpdate);

            var observeRemoved = Observable.FromEvent<EPage_BeforeShapeDeleteEventHandler, IVShape>(
                    handler => page.BeforeShapeDelete += handler,
                    handler => page.BeforeShapeDelete -= handler,
                    SchedulerManager.VisioScheduler)
                .Where(predicate)
                .Select(shape =>
                    new VisioShape(new VisioShapeId(shape.ContainingPageID, shape.ID), GetShapeTypes(shape)))
                .Do(x => DebugExt.Log("BeforeShapeDelete", x))
                .Do(visioShape => cache.RemoveKey(visioShape.Id));

            var observeCellUpdated = Observable.FromEvent<EPage_CellChangedEventHandler, Cell>(
                    handler => page.CellChanged += handler,
                    handler => page.CellChanged -= handler,
                    SchedulerManager.VisioScheduler)
                .Where(cell => CellValuesToMonitor.Contains(cell.Name));
            var observeFormulaUpdated = Observable.FromEvent<EPage_FormulaChangedEventHandler, Cell>(
                    handler => page.FormulaChanged += handler,
                    handler => page.FormulaChanged -= handler,
                    SchedulerManager.VisioScheduler)
                .Where(cell => cell.Name == CellDict.Relationships);
            var observeUpdated = observeCellUpdated
                .Merge(observeFormulaUpdated)
                .Where(x => predicate(x.Shape))
                .QuiescentBuffer(TimeSpan.FromMilliseconds(400), SchedulerManager.VisioScheduler)
                .SelectMany(x => x.GroupBy(i => i.Shape.ID)
                    .Select(i =>
                        {
                            var shape = i.First().Shape;
                            return new VisioShape(new VisioShapeId(shape.ContainingPageID, shape.ID),
                                GetShapeTypes(shape))
                            {
                                ChangedProperties = i.Select(t => t.LocalName).ToArray()
                            };
                        }
                    ))
                .Do(x => DebugExt.Log("CellChanged", x))
                .Do(cache.AddOrUpdate);

            var subscription = Observable.Merge(observeAdded, observeRemoved, observeUpdated)
                .Subscribe();

            // load initial values
            // todo：最理想的情况是在需要的时候才加载
            // todo: 2025.02.03: 此处有一个已知问题，这个方法无法识别Group中的shape。如果再去递归判断Group中的Shapes，逻辑上太复杂了。必须要向用户澄清这种行为是不允许的。但是用户本身可能有复用的诉求，该怎么处理？
            var initials = page.Shapes.OfType<IVShape>()
                .Where(predicate)
                .Select(shape =>
                    new VisioShape(new VisioShapeId(shape.ContainingPageID, shape.ID), GetShapeTypes(shape))).ToList();
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
        var locationId = new VisioShapeId(source.ContainingPageID, source.ID);

        var materialCode = source.TryGetValue(CellDict.MaterialCode) ?? string.Empty;
        var unitQuantity = source.TryGetValue<double>(CellDict.UnitQuantity) ?? 0;
        var quantity = source.TryGetValue<int>(CellDict.Quantity) ?? 0;

        var keyParameters = string.Empty;
        if (source.CellExistsN(CellDict.KeyParameters, VisExistsFlags.visExistsAnywhere))
            keyParameters = source.TryGetValue(CellDict.KeyParameters) ?? string.Empty;

        var type = source.TryGetValue(CellDict.SubClass) ?? string.Empty;

        return new MaterialLocation(locationId, materialCode, unitQuantity, quantity, type,
            keyParameters);
    }

    /// <summary>
    ///     Convert a <see cref="IVShape" /> to <see cref="MaterialLocation" />.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static Instrument ToInstrument(this IVShape source)
    {
        var locationId = new VisioShapeId(source.ContainingPageID, source.ID);

        var materialCode = source.TryGetValue(CellDict.MaterialCode) ?? string.Empty;
        var unitQuantity = source.TryGetValue<double>(CellDict.UnitQuantity) ?? 0;
        var quantity = source.TryGetValue<int>(CellDict.Quantity) ?? 0;

        // todo: get high, low
        var high = string.Empty;
        var low = string.Empty;
        var type = source.TryGetValue(CellDict.SubClass) ?? string.Empty;

        return new Instrument(locationId, materialCode, unitQuantity, quantity,
            type, high, low);
    }

    private static LocationType[] GetShapeTypes(this IVShape x)
    {
        var shapeCategories = x.TryGetValue(CellDict.ShapeCategories);
        if (shapeCategories == null) return [LocationType.None];

        return shapeCategories switch
        {
            "Frame" or "FunctionalGroup" or "Unit" or "Exclude" => [LocationType.FunctionLocation],
            "Equipment" or "Equipments" or "Instrument" or "Instruments" or "FunctionalElement"
                or "FunctionalElements" =>
                [
                    LocationType.FunctionLocation, LocationType.MaterialLocation
                ],
            _ => [LocationType.None]
        };
    }
}