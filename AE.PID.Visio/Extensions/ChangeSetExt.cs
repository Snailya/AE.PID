﻿using System;
using System.Collections.Generic;
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
    private static readonly string[] CellsToMonitor =
    {
        CellNameDict.FunctionZone, CellNameDict.FunctionZoneName, CellNameDict.FunctionZoneEnglishName,
        CellNameDict.FunctionGroup, CellNameDict.FunctionGroupName, CellNameDict.FunctionZoneEnglishName,
        CellNameDict.FunctionGroupDescription,
        CellNameDict.FunctionElement, CellNameDict.ElementName, CellNameDict.Description,
        CellNameDict.Remarks,
        CellNameDict.SubClass, CellNameDict.KeyParameters, CellNameDict.UnitQuantity, CellNameDict.Quantity,
        CellNameDict.MaterialCode
    };

    public static IObservable<IChangeSet<string, string>> ToMasterChangeSet(
        this Document document)
    {
        return ObservableChangeSet.Create<string, string>(cache =>
        {
            var observeAdded = Observable.FromEvent<EDocument_MasterAddedEventHandler, Master>(
                    handler => document.MasterAdded += handler,
                    handler => document.MasterAdded -= handler,
                    SchedulerManager.VisioScheduler)
                .Select(master => master.BaseID)
                .Do(x => LogHost.Default.Debug($"MasterAdded {x}"))
                .Do(cache.AddOrUpdate);

            var observeRemoved = Observable.FromEvent<EDocument_BeforeMasterDeleteEventHandler, Master>(
                    handler => document.BeforeMasterDelete += handler,
                    handler => document.BeforeMasterDelete -= handler,
                    SchedulerManager.VisioScheduler)
                .Select(master => master.BaseID)
                .Do(x => LogHost.Default.Debug($"BeforeMasterDelete {x}"))
                .Do(cache.RemoveKey);

            var subscription = observeAdded.Merge(observeRemoved)
                .Subscribe();

            // load initial values
            var initials = document.Masters.OfType<IVMaster>()
                .Select(x => x.BaseID)
                .ToList();
            cache.AddOrUpdate(initials);

            return subscription;
        }, t => t);
    }

    public static IObservable<IChangeSet<CompositeId, CompositeId>> ToShapeChangeSet(
        this Document document)
    {
        return ObservableChangeSet.Create<CompositeId, CompositeId>(cache =>
            {
                var subscription = new CompositeDisposable();

                // observe each page's change
                document.Pages.OfType<Page>().ToObservable()
                    .Merge(Observable
                        .FromEvent<EDocument_PageAddedEventHandler, Page>(
                            handler => document.PageAdded += handler,
                            handler => document.PageAdded -= handler,
                            SchedulerManager.VisioScheduler))
                    .Subscribe(page =>
                    {
                        var observePageChange = page.ToChangeSet()
                            .OnItemAdded(x => LogHost.Default.Debug($"Detect ChangeSet Added: {x}"))
                            .OnItemRemoved(x => LogHost.Default.Debug($"Detect ChangeSet Removed: {x}"))
                            .OnItemRefreshed(x => LogHost.Default.Debug($"Detect ChangeSet Refreshed: {x}"))
                            .OnItemUpdated((current, previous) =>
                                LogHost.Default.Debug($"Detect ChangeSet Updated: {current}"))
                            .PopulateInto(cache)
                            .DisposeWith(subscription);

                        Observable.FromEvent<EPage_BeforePageDeleteEventHandler, Page>(
                                handler => page.BeforePageDelete += handler,
                                handler => page.BeforePageDelete -= handler,
                                SchedulerManager.VisioScheduler)
                            .Subscribe(x =>
                            {
                                var items = cache.Items.Where(i => i.PageId == x.ID);
                                LogHost.Default.Debug(
                                    $"Page deleted, going to remove {items.Count()} items from changeset.");
                                cache.RemoveKeys(items); // remove items
                                observePageChange.Dispose(); // Dispose observe the shape change on that page
                            })
                            .DisposeWith(subscription);
                    }).DisposeWith(subscription);
                // Return the Disposable that controls the subscription
                return subscription;
            },
            f => f);
    }

    private static FunctionType GetFunctionType(IVShape source)
    {
        return source.HasCategory("Frame") ? FunctionType.ProcessZone :
            source.HasCategory("FunctionalGroup") ? FunctionType.FunctionGroup :
            source.HasCategory("Unit") ? FunctionType.FunctionUnit :
            source.HasCategory("Equipment") ? FunctionType.Equipment :
            source.HasCategory("Instrument") ? FunctionType.Instrument :
            source.HasCategory("FunctionalElement") ? FunctionType.FunctionElement :
            throw new ArgumentException();
    }

    /// <summary>
    ///     Convert a <see cref="IVMaster" /> to <see cref="Symbol" />.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static Symbol ToSymbol(this IVMaster source)
    {
        return new Symbol
        {
            Id = source.BaseID,
            Name = source.Name
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
        var id = new CompositeId(source.ContainingPageID, source.ID);

        var type = GetFunctionType(source);

        var functionIdStr = source.TryGetValue("User.FunctionId");
        var functionId = double.TryParse(functionIdStr, out var functionIdDouble) ? (int)functionIdDouble : 0;

        var containers = source.MemberOfContainers.OfType<int>().Select(x => source.ContainingPage.Shapes.ItemFromID[x])
            .Select(x => new { Type = GetFunctionType(x), Id = new CompositeId(x.ContainingPageID, x.ID) })
            .ToArray();
        var parentId = containers.Where(x => x.Type <= type).OrderBy(x => x.Type).LastOrDefault()?.Id ??
                       new CompositeId(source.ContainingPageID);

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
            _ => throw new ArgumentOutOfRangeException()
        } ?? string.Empty;

        var remarks = source.TryGetValue(CellNameDict.Remarks) ?? string.Empty;

        var description = type switch
        {
            FunctionType.ProcessZone => string.Empty,
            FunctionType.FunctionGroup => source.TryGetValue(CellNameDict.FunctionGroupDescription),
            FunctionType.FunctionUnit => source.TryGetValue(CellNameDict.FunctionGroupDescription),
            FunctionType.Equipment => source.TryGetValue(CellNameDict.Description),
            FunctionType.Instrument => source.TryGetValue(CellNameDict.Description),
            FunctionType.FunctionElement => source.TryGetValue(CellNameDict.Description),
            _ => throw new ArgumentOutOfRangeException()
        } ?? string.Empty;

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
            Description = description
        };
    }

    private static IObservable<IChangeSet<CompositeId, CompositeId>> ToChangeSet(
        this Page page, Func<IVShape, bool>? predicate = null)
    {
        predicate ??= x => true;

        return ObservableChangeSet.Create<CompositeId, CompositeId>(cache =>
        {
            var observeAdded = Observable.FromEvent<EPage_ShapeAddedEventHandler, IVShape>(
                    handler => page.ShapeAdded += handler,
                    handler => page.ShapeAdded -= handler,
                    SchedulerManager.VisioScheduler)
                .Where(predicate)
                .Select(shape => new CompositeId(shape.ContainingPageID, shape.ID))
                .Do(x => LogHost.Default.Debug($"ShapeAdded {x}"))
                .Do(cache.AddOrUpdate);

            var observeRemoved = Observable.FromEvent<EPage_BeforeShapeDeleteEventHandler, IVShape>(
                    handler => page.BeforeShapeDelete += handler,
                    handler => page.BeforeShapeDelete -= handler,
                    SchedulerManager.VisioScheduler)
                .Where(predicate)
                .Select(shape => new CompositeId(shape.ContainingPageID, shape.ID))
                .Do(x => LogHost.Default.Debug($"BeforeShapeDelete {x}"))
                .Do(cache.RemoveKey);

            var observeUpdated =
                Observable.FromEvent<EPage_CellChangedEventHandler, Cell>(
                        handler => page.CellChanged += handler,
                        handler => page.CellChanged -= handler,
                        SchedulerManager.VisioScheduler)
                    .Where(cell => CellsToMonitor.Contains(cell.Name))
                    .Select(cell => cell.Shape)
                    .Where(predicate)
                    .Select(shape => new CompositeId(shape.ContainingPageID, shape.ID))
                    .QuiescentBuffer(TimeSpan.FromMilliseconds(400), SchedulerManager.VisioScheduler)
                    .SelectMany(x => x.Distinct())
                    .Do(x => LogHost.Default.Debug($"ShapeCellChanged {x}"))
                    .Do(cache.AddOrUpdate);

            var subscription = Observable.Merge(observeAdded, observeRemoved, observeUpdated)
                .Subscribe();

            // load initial values
            var initials = page.Shapes.OfType<IVShape>()
                .Where(predicate)
                .Select(x => new CompositeId(x.ContainingPageID, x.ID))
                .ToList();
            cache.AddOrUpdate(initials);

            return subscription;
        }, t => t);
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
            UnitQuantity = unitQuantity,
            Quantity = quantity,
            Code = materialCode ?? string.Empty,
            KeyParameters = keyParameters ?? string.Empty,
            Type = type ?? string.Empty
        };
    }

    public static IEnumerable<IVShape> IsFunctionLocation(this IEnumerable<IVShape> source)
    {
        return source.Where(IsFunctionLocation);
    }

    public static bool IsFunctionLocation(this IVShape x)
    {
        return x.HasCategory("Frame") ||
               (x.HasCategory("FunctionalGroup") && !x.HasCategory("Proxy")) ||
               x.HasCategory("Unit") ||
               x.HasCategory("Equipment") ||
               x.HasCategory("Instrument") ||
               x.HasCategory("FunctionalElement");
    }

    public static IEnumerable<IVShape> IsMaterialLocation(this IEnumerable<IVShape> source)
    {
        return source.Where(IsMaterialLocation);
    }

    public static bool IsMaterialLocation(this IVShape x)
    {
        return x.HasCategory("Equipment") ||
               x.HasCategory("Instrument") ||
               x.HasCategory("FunctionalElement");
    }
}