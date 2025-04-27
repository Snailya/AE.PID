using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Client.Core;
using AE.PID.Client.Core.VisioExt;
using AE.PID.Client.Infrastructure;
using AE.PID.Client.Infrastructure.VisioExt;
using DynamicData;
using Microsoft.Office.Interop.Visio;
using Splat;

namespace AE.PID.Client.VisioAddIn;

public static class ChangeSetExt
{
    private static readonly TimeSpan BufferTime = TimeSpan.FromMilliseconds(400);
    
    public static readonly string[] FunctionLocationCellValuesToMonitor =
    {
        CellDict.FunctionZone, CellDict.FunctionZoneName, CellDict.FunctionZoneEnglishName,
        CellDict.FunctionGroup, CellDict.FunctionGroupName, CellDict.FunctionZoneEnglishName,
        CellDict.FunctionGroupDescription,
        CellDict.FunctionElement, CellDict.ElementName,
        CellDict.Description,
        CellDict.Remarks,
        CellDict.RefEquipment,
        CellDict.IsSelectedInProject
    };

    public static readonly string[] MaterialCellsToMonitor =
    {
        CellDict.SubClass, CellDict.KeyParameters, CellDict.UnitQuantity, CellDict.Quantity,
        CellDict.MaterialCode,
        CellDict.Customer
    };

    private static readonly IEnumerable<string> CellValuesToMonitor =
        FunctionLocationCellValuesToMonitor.Concat(MaterialCellsToMonitor);

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
                .Do(cache.AddOrUpdate);

            var observeRemoved = Observable.FromEvent<EDocument_BeforeMasterDeleteEventHandler, Master>(
                    handler => document.BeforeMasterDelete += handler,
                    handler => document.BeforeMasterDelete -= handler,
                    SchedulerManager.VisioScheduler)
                .Select(master => new VisioMaster(master.BaseID, master.Name, master.UniqueID))
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
    /// <param name="predicate"></param>
    /// <returns></returns>
    public static IObservable<IChangeSet<VisioShape, VisioShapeId>> ToShapeChangeSet(
        this Document document, Func<IVShape, bool>? predicate = null)
    {
        return ObservableChangeSet.Create<VisioShape, VisioShapeId>(cache =>
            {
                var subscription = new CompositeDisposable();

                document.Pages.OfType<Page>().ToObservable()
                    .Merge(Observable
                        .FromEvent<EDocument_PageAddedEventHandler, Page>(
                            handler => document.PageAdded += handler,
                            handler => document.PageAdded -= handler,
                            SchedulerManager.VisioScheduler)) // 2025.3.19 观察新增页面
                    .Subscribe(page =>
                    {
                        var observePageChange = page.ToChangeSet(predicate)
#if DEBUG
                            .DebugLog()
#endif
                            .PopulateInto(cache)
                            .DisposeWith(subscription);

                        Observable.FromEvent<EPage_BeforePageDeleteEventHandler, Page>(
                                handler => page.BeforePageDelete += handler,
                                handler => page.BeforePageDelete -= handler,
                                SchedulerManager.VisioScheduler) // 2025.3.19 观察删除页面
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
                // 2025.03.28： 由于shape是COM对象，必须先提取其属性，否则在buffer后可能已经释放
                .Select(shape => new VisioShape(new VisioShapeId(shape.ContainingPageID, shape.ID),
                    shape.GetCategories()))
                .Buffer(BufferTime)
                .Where(x => x.Count > 0)
                .Select(shapes =>
                {
                    cache.Edit(updater => updater.AddOrUpdate(shapes));
                    return Unit.Default;
                });

            var observeRemoved = Observable.FromEvent<EPage_BeforeShapeDeleteEventHandler, IVShape>(
                    handler => page.BeforeShapeDelete += handler,
                    handler => page.BeforeShapeDelete -= handler,
                    SchedulerManager.VisioScheduler)
                .Where(predicate)
                .Select(x => new VisioShapeId(x.ContainingPageID, x.ID))
                .Buffer(BufferTime)
                .Where(x => x.Count > 0)
                .Select(ids =>
                {
                    cache.Edit(updater =>
                        updater.RemoveKeys(ids));
                    return Unit.Default;
                });

            var observeCellUpdated = Observable.FromEvent<EPage_CellChangedEventHandler, Cell>(
                    handler => page.CellChanged += handler,
                    handler => page.CellChanged -= handler,
                    SchedulerManager.VisioScheduler)
                .Where(cell => CellValuesToMonitor.Contains(cell.Name));
            var observeRelationshipChanged = Observable
                .FromEvent<EPage_ContainerRelationshipAddedEventHandler, RelatedShapePairEvent>(
                    handler => page.ContainerRelationshipAdded += handler,
                    handler => page.ContainerRelationshipAdded -= handler,
                    SchedulerManager.VisioScheduler)
                .Merge(Observable.FromEvent<EPage_ContainerRelationshipDeletedEventHandler, RelatedShapePairEvent>(
                    handler => page.ContainerRelationshipDeleted += handler,
                    handler => page.ContainerRelationshipDeleted -= handler,
                    SchedulerManager.VisioScheduler))
                .Select(relationshipPair =>
                    {
                        var fromShape = relationshipPair.ContainingPage.Shapes.ItemFromID[relationshipPair.FromShapeID];
                        var toShape = relationshipPair.ContainingPage.Shapes.ItemFromID[relationshipPair.ToShapeID];

                        if (fromShape.IsCallout)
                            return new VisioShape(new VisioShapeId(relationshipPair.ContainingPageID,
                                relationshipPair.FromShapeID), fromShape.GetCategories());
                        return new VisioShape(new VisioShapeId(relationshipPair.ContainingPageID,
                            relationshipPair.ToShapeID), toShape.GetCategories());
                    }
                )
                .Buffer(BufferTime)
                .Where(shapes => shapes.Count > 0)
                .Select(shapes =>
                {
                    cache.Edit(updater => updater.AddOrUpdate(shapes));
                    return Unit.Default;
                });

            // 仅观察relationship会导致容器内对象增加或减少时引发容器对象不必要的更新
            var observeUpdated = observeCellUpdated
                .Where(x => predicate(x.Shape))
                .QuiescentBuffer(TimeSpan.FromMilliseconds(400), SchedulerManager.VisioScheduler)
                .Select(x =>
                {
                    cache.Edit(updater =>
                        updater.AddOrUpdate(x.GroupBy(i => i.Shape.ID)
                            .Select(i =>
                                {
                                    var shape = i.First().Shape;
                                    return new VisioShape(new VisioShapeId(shape.ContainingPageID, shape.ID),
                                        shape.GetCategories())
                                    {
                                        ChangedProperties = i.Select(t => t.LocalName).ToArray()
                                    };
                                }
                            )));

                    return Unit.Default;
                });

            var subscription = Observable
                .Merge(observeAdded, observeRemoved, observeUpdated, observeRelationshipChanged)
                .Subscribe();

            // load initial values
            // todo：最理想的情况是在需要的时候才加载
            // todo: 2025.02.03: 此处有一个已知问题，这个方法无法识别Group中的shape。如果再去递归判断Group中的Shapes，逻辑上太复杂了。必须要向用户澄清这种行为是不允许的。但是用户本身可能有复用的诉求，该怎么处理？
            var initials = page.Shapes.OfType<IVShape>()
                .Where(predicate)
                .Select(shape =>
                    new VisioShape(new VisioShapeId(shape.ContainingPageID, shape.ID), shape.GetCategories()))
                .ToList();
            cache.AddOrUpdate(initials);

            return subscription;
        }, t => t.Id);
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
            type, high, low, false);
    }
}