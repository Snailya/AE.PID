using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using AE.PID.Client.Core;
using AE.PID.Client.Core.VisioExt;
using AE.PID.Client.Infrastructure;
using AE.PID.Core;
using DynamicData;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Client.VisioAddIn;

internal class FunctionLocationProcessor : IDisposable
{
    private readonly CompositeDisposable _cleanUp = new();

    private readonly VisioDocumentService _docService;
    private readonly OverlayProcessor _overlayProcessor;
    private readonly SourceCache<FunctionLocation, ICompoundKey> _realFunctionLocation = new(x => x.Id);
    private readonly Subject<FunctionLocation[]> _updater = new();
    private readonly VirtualLocationGenerator _virtualLocationGenerator;

    public FunctionLocationProcessor(VisioDocumentService docService, OverlayProcessor overlayProcessor,
        IObservableCache<VisioShape, VisioShapeId> shapes)
    {
        _docService = docService;
        _overlayProcessor = overlayProcessor;
        _virtualLocationGenerator = new VirtualLocationGenerator(_realFunctionLocation);

        // convert the shapes to function locations and material locations. 
        // as the _shapes are lazily loaded and controlled by an external call, this transformation will not bring delay time when initialized, so that the UI will not block
        // 2025.3.26: Shapes中的一部分是Function Location
        shapes
            .Connect()
            .Filter(x => x.IsFunctionLocation)
            .Transform(ToFunctionLocation)
            // after the shape is transformed into the model, switch the forward working into background scheduler
            .ChangeKey(x => x.Id)
            .ObserveOn(TaskPoolScheduler.Default)
            .PopulateInto(_realFunctionLocation)
            .DisposeWith(_cleanUp);


        var observeOverlay = _overlayProcessor.Cache.Connect();
        Locations = _realFunctionLocation.Connect()
            .Merge(_virtualLocationGenerator.VirtualLocations.Connect()
                .ChangeKey(x => new VirtualLocationKey((VisioShapeId)x.ProxyGroupId!, (VisioShapeId)x.TargetId!))
                .LeftJoin(observeOverlay,
                    overlay => overlay.Key,
                    (functionLocation, overlay) => overlay.HasValue
                        ? OverlayProcessor.ApplyOverlay(functionLocation, overlay.Value)
                        : functionLocation
                )
                .ChangeKey(x => x.Id))
            .Transform(EnsureParentIdNotNull)
            .Batch(TimeSpan.FromMilliseconds(400))
#if DEBUG
            .DebugLog(nameof(FunctionLocationProcessor))
#endif
            .AsObservableCache();

        _updater
            .SelectMany(x => x)
            .Select(BuildPropertyPatch)
            .Subscribe(docService.UpdateProperties)
            .DisposeWith(_cleanUp);
    }

    /// <summary>
    ///     Provides function locations include both actual location and virtual locations
    /// </summary>
    public IObservableCache<FunctionLocation, ICompoundKey> Locations { get; }

    /// <summary>
    ///     Dispose the process for creating the virtual location and the converting from shape to function location.
    /// </summary>
    public void Dispose()
    {
        _virtualLocationGenerator.Dispose();
        _cleanUp.Dispose();
    }

    /// <summary>
    ///     Update the parent id to make it not null, otherwise the dynamic data will throw exception when transformtotree.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private static FunctionLocation EnsureParentIdNotNull(FunctionLocation x)
    {
        if (x.ParentId != null) return x;

        // if it is the root, which means it is either a zone, or other items outside the zone
        return x with { ParentId = VisioShapeId.Default };
    }

    /// <summary>
    ///     Update the function locations.
    /// </summary>
    /// <param name="locations"></param>
    public void Update(FunctionLocation[] locations)
    {
        _updater.OnNext(locations.Where(x => !x.IsVirtual).ToArray());

        // 分流
        _overlayProcessor.Write(
            locations
                .Where(x => x.IsVirtual)
                .Select(current =>
                {
                    var source = _realFunctionLocation.Lookup(current.TargetId!).Value;

                    var key = new VirtualLocationKey((VisioShapeId)current.ProxyGroupId!,
                        (VisioShapeId)current.TargetId!);
                    var overlay = _overlayProcessor.Cache.Lookup(key);

                    // 有四种情况：
                    // 1. 没有Overlay，值与source一样: 不操作
                    // 2. 没有Overlay，值与source不一样：新建Overlay并填入值
                    // 3. 有Overlay，值与source一样：删除Overlay 
                    // 4. 有Overlay，值与source不一样：覆盖值

                    var hasOverlay = overlay.HasValue;
                    var isValueEqual = source.Description == current.Description && source.Remarks == current.Remarks &&
                                       source.UnitMultiplier == current.UnitMultiplier;

                    if (!hasOverlay && isValueEqual)
                        return null;

                    var overlayValue = overlay.HasValue ? overlay.Value : new LocationOverlay(key);
                    overlayValue.Description = source.Description == current.Description ? null : current.Description;
                    overlayValue.Remarks = source.Remarks == current.Remarks ? null : current.Remarks;
                    overlayValue.UnitMultiplier =
                        source.UnitMultiplier == current.UnitMultiplier ? null : current.UnitMultiplier;

                    return overlayValue;
                })
                .Where(x => x != null)
                .Cast<LocationOverlay>()
                .ToArray()
        );
    }

    /// <summary>
    ///     Convert <see cref="VisioShapeCategory" /> collection to <see cref="FunctionType" />.
    /// </summary>
    /// <param name="categories"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private static FunctionType MapCategoriesToFunctionType(ICollection<VisioShapeCategory> categories)
    {
        if (categories.Contains(VisioShapeCategory.ProcessZone)) return FunctionType.ProcessZone;
        if (categories.Contains(VisioShapeCategory.Unit)) return FunctionType.FunctionUnit;
        if (categories.Contains(VisioShapeCategory.FunctionalGroup)) return FunctionType.FunctionGroup;
        if (categories.Contains(VisioShapeCategory.Equipment)) return FunctionType.Equipment;
        if (categories.Contains(VisioShapeCategory.Instrument)) return FunctionType.Instrument;
        if (categories.Contains(VisioShapeCategory.FunctionalElement)) return FunctionType.FunctionElement;

        throw new ArgumentException($"Can't find any function type that matches these categories: {categories}");
    }

    /// <summary>
    ///     Convert a <see cref="IVShape" /> to <see cref="FunctionLocation" />.
    /// </summary>
    /// <param name="shape"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private FunctionLocation ToFunctionLocation(VisioShape shape)
    {
        var source = _docService.GetShape(shape.Id);

        var id = new VisioShapeId(source.ContainingPageID, source.ID);

        var functionType = MapCategoriesToFunctionType(shape.Categories);

        var functionIdStr = source.TryGetValue(CellDict.FunctionId);
        var functionId = double.TryParse(functionIdStr, out var functionIdDouble) ? (int)functionIdDouble : 0;

        // 2025.02.13：如果是一般对象，则用容器判断Parent，如果是FunctionElement，则用Callout判断
        // 2025.03.24: 现在对于代理功能组，它的Parent不再是它实际的节点
        var isProxy = source.IsCallout;

        VisioShapeId? parentId;
        VisioShapeId? target = null;

        // 分成以下集中情况：
        // - Zone：null
        // - Group
        //      - Non-Proxy: 根据实际值
        //      - Proxy: null
        // - Unit,Equipment,Instrument: 根据实际值
        // - Element: CalloutTarget值

        switch (functionType)
        {
            case FunctionType.ProcessZone:
                parentId = null;
                break;
            case FunctionType.FunctionGroup:
                if (isProxy)
                {
                    parentId = new VisioShapeId(-1);
                    target = new VisioShapeId(source.ContainingPageID, source.CalloutTarget.ID);
                }
                else
                {
                    parentId = GetContainerId(source);
                }

                break;
            case FunctionType.FunctionUnit:
            case FunctionType.Equipment:
            case FunctionType.Instrument:
                parentId = GetContainerId(source);
                break;
            case FunctionType.FunctionElement:
                parentId = new VisioShapeId(source.ContainingPageID, source.CalloutTarget.ID);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        var zone = source.TryGetFormatValue(CellDict.FunctionZone) ?? string.Empty;
        var zoneName = source.TryGetFormatValue(CellDict.FunctionZoneName) ?? string.Empty;
        var zoneEnglishName = source.TryGetFormatValue(CellDict.FunctionZoneEnglishName) ?? string.Empty;

        var group = source.TryGetFormatValue(CellDict.FunctionGroup) ?? string.Empty;
        var groupName = source.TryGetFormatValue(CellDict.FunctionGroupName) ?? string.Empty;
        var groupEnglishName = source.TryGetFormatValue(CellDict.FunctionGroupEnglishName) ?? string.Empty;

        var element = functionType switch
        {
            FunctionType.ProcessZone => string.Empty,
            FunctionType.FunctionGroup => string.Empty,
            FunctionType.FunctionUnit => string.Empty,
            FunctionType.Equipment => source.TryGetFormatValue(CellDict.FunctionElement),
            FunctionType.Instrument => source.TryGetFormatValue(CellDict.FunctionElement),
            FunctionType.FunctionElement => source.TryGetValue(CellDict.RefEquipment) + "-" +
                                            source.TryGetFormatValue(CellDict.FunctionElement),
            _ => throw new ArgumentOutOfRangeException()
        } ?? string.Empty;

        var remarks = source.TryGetValue(CellDict.Remarks) ?? string.Empty;

        var description = functionType switch
        {
            FunctionType.FunctionGroup => source.TryGetValue(CellDict.FunctionGroupDescription),
            FunctionType.FunctionUnit => source.TryGetValue(CellDict.FunctionGroupDescription),
            FunctionType.Equipment => source.TryGetValue(CellDict.Description),
            FunctionType.Instrument => source.TryGetValue(CellDict.Description),
            FunctionType.FunctionElement => source.TryGetValue(CellDict.Description),
            _ => string.Empty
        } ?? string.Empty;

        var unitMultiplier = functionType == FunctionType.FunctionUnit
            ? source.TryGetValue<int>(CellDict.UnitQuantity) ?? 1
            : 1;

        // 2025.04.21: a location can be confiugred as included in a project already or not yet to reveal the client to consider including it.
        // if a location is configured as already included, it should be displayed in the project explorer.
        var isIncludeInProject = source.TryGetValue<bool>(CellDict.IsSelectedInProject) ?? true;

        return new FunctionLocation(id,
            parentId,
            functionType,
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
            string.Empty,
            unitMultiplier,
            isIncludeInProject,
            isProxy,
            target,
            false
        );
    }

    private static VisioShapeId? GetContainerId(Shape source)
    {
        var parent = source.MemberOfContainers.OfType<int>().Select(x =>
                new
                {
                    Id = x, ContainerCount = source.ContainingPage.Shapes.ItemFromID[x].MemberOfContainers.Length
                })
            .OrderByDescending(x => x.ContainerCount)
            .FirstOrDefault();
        var parentId = parent == null ? null : new VisioShapeId(source.ContainingPageID, parent.Id);
        return parentId;
    }

    /// <summary>
    ///     Build up the property patches based on the function location type.
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    private static IEnumerable<PropertyPatch> BuildPropertyPatch(FunctionLocation location)
    {
        var patches = new List<PropertyPatch>();

        switch (location.Type)
        {
            case FunctionType.Equipment:
                var elementNumber = Regex.Match(location.Element, @"\d+").Value;
                patches.AddRange([
                    new PropertyPatch(location.Id, CellDict.FunctionElement, elementNumber),
                    new PropertyPatch(location.Id, CellDict.Description, location.Description)
                ]);
                break;
            // 2025.02.13: 仪表和功能单元不应该仅提取数字
            case FunctionType.Instrument:
                patches.AddRange([
                    new PropertyPatch(location.Id, CellDict.FunctionElement, location.Element),
                    new PropertyPatch(location.Id, CellDict.Description, location.Description)
                ]);
                break;
            // 2025.02.13：function element 取最后一个“-”后面的全部内容
            case FunctionType.FunctionElement:
                var element = Regex.Match(location.Element, @"[^-]+$").Value;
                patches.AddRange([
                    new PropertyPatch(location.Id, CellDict.FunctionElement, element),
                    new PropertyPatch(location.Id, CellDict.Description, location.Description)
                ]);
                break;
            case FunctionType.FunctionGroup:
                patches.AddRange([
                    new PropertyPatch(location.Id, CellDict.FunctionGroup, location.Group),
                    new PropertyPatch(location.Id, CellDict.FunctionGroupName, location.GroupName),
                    new PropertyPatch(location.Id, CellDict.FunctionGroupEnglishName,
                        location.GroupEnglishName),
                    new PropertyPatch(location.Id, CellDict.FunctionGroupDescription,
                        location.Description)
                ]);

                break;
            case FunctionType.ProcessZone:
                patches.AddRange([
                    new PropertyPatch(location.Id, CellDict.FunctionZone, location.Zone),
                    new PropertyPatch(location.Id, CellDict.FunctionZoneName, location.ZoneName),
                    new PropertyPatch(location.Id, CellDict.FunctionZoneEnglishName,
                        location.ZoneEnglishName)
                ]);
                break;
        }

        if (location.FunctionId != null)
            patches.Add(new PropertyPatch(location.Id, CellDict.FunctionId, location.FunctionId, true));
        patches.Add(new PropertyPatch(location.Id, CellDict.Remarks, location.Remarks, true, "\"备注\""));

        return patches;
    }
}