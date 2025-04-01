using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using AE.PID.Client.Core;
using AE.PID.Client.Core.VisioExt;
using AE.PID.Client.Infrastructure;
using AE.PID.Core;
using DynamicData;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Client.VisioAddIn;

internal class MaterialLocationProcessor : IDisposable
{
    private const double FloatTolerance = 0.01;
    private readonly CompositeDisposable _cleanUp = new();

    private readonly VisioDocumentService _docService;
    private readonly OverlayProcessor _overlayProcessor;
    private readonly Subject<MaterialLocation[]> _updater = new();

    public MaterialLocationProcessor(VisioDocumentService docService,
        OverlayProcessor overlayProcessor,
        IObservableCache<FunctionLocation, ICompoundKey> functionLocations)
    {
        _docService = docService;
        _overlayProcessor = overlayProcessor;

        var materialLocations = functionLocations.Connect()
            // 先过滤出需要处理的FunctionLocation类型
            .Filter(x => x.Type is FunctionType.Equipment or FunctionType.Instrument or FunctionType.FunctionElement)
            .Transform(ToMaterialLocation);

        var virtualLocations = materialLocations
            .Filter(x => x.IsVirtual)
            .ChangeKey(x => new VirtualLocationKey((VisioShapeId)x.ProxyGroupId!, (VisioShapeId)x.TargetId!))
            .LeftJoin(
                overlayProcessor.Cache.Connect(),
                overlay => overlay.Key,
                (materialLocation, overlay) => overlay.HasValue
                    ? OverlayProcessor.ApplyOverlay(materialLocation, overlay.Value)
                    : materialLocation
            )
            .ChangeKey(x => x.Id);

        // 处理非虚拟Location（直接转换）
        var realLocations = materialLocations
            .Filter(x => !x.IsVirtual);

        Locations = realLocations.Merge(virtualLocations)
            .ObserveOn(TaskPoolScheduler.Default)
            .AsObservableCache();

        _updater
            .SelectMany(x => x)
            .Select(BuildPropertyPatch)
            .Subscribe(docService.UpdateProperties)
            .DisposeWith(_cleanUp);
    }

    /// <summary>
    ///     Provides the material locations include both actual and virtual.
    /// </summary>
    public IObservableCache<MaterialLocation, ICompoundKey> Locations { get; }

    /// <summary>
    ///     Dispose the process for converting the function location to material location.
    /// </summary>
    public void Dispose()
    {
        _cleanUp.Dispose();
    }

    /// <summary>
    ///     Convert a <see cref="FunctionLocation" /> to <see cref="MaterialLocation" />.
    /// </summary>
    /// <param name="functionLocation"></param>
    /// <returns></returns>
    private MaterialLocation ToMaterialLocation(FunctionLocation functionLocation)
    {
        // 2025.03.26: 如果当前功能位是虚拟的，读取其数据的source应该是Target
        var shapeId = (VisioShapeId)(functionLocation.IsVirtual ? functionLocation.TargetId : functionLocation.Id)!;
        var source = _docService.GetShape(shapeId);

        var locationId = functionLocation.Id;

        var materialCode = source.TryGetValue(CellDict.MaterialCode) ?? string.Empty;
        var unitQuantity = source.TryGetValue<int>(CellDict.UnitQuantity) ?? 0;
        var quantity = source.TryGetValue<int>(CellDict.Quantity) ?? 0;
        var unitMultiplier = unitQuantity==0? 1:quantity / unitQuantity;

        var keyParameters = string.Empty;
        if (source.CellExistsN(CellDict.KeyParameters, VisExistsFlags.visExistsAnywhere))
            keyParameters = source.TryGetValue(CellDict.KeyParameters) ?? string.Empty;

        var type = source.TryGetValue(CellDict.SubClass) ?? string.Empty;

        var material = new MaterialLocation(locationId, materialCode, unitQuantity, unitMultiplier, type,
            keyParameters, functionLocation.IsVirtual, functionLocation.ProxyGroupId, functionLocation.TargetId);

        return material;
    }

    /// <summary>
    ///     Extract the settable part of the material location's values and convert them to the property patch array to update
    ///     the shape cell in Visio
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    private static IEnumerable<PropertyPatch> BuildPropertyPatch(MaterialLocation location)
    {
        return
        [
            new PropertyPatch(location.Id, CellDict.MaterialCode, location.Code, true),
            new PropertyPatch(location.Id, CellDict.UnitQuantity, location.Quantity)
        ];
    }

    /// <summary>
    ///     Update the material locations.
    /// </summary>
    /// <param name="locations"></param>
    public void Update(MaterialLocation[] locations)
    {
        // 分流
        _updater.OnNext(locations.Where(x => !x.IsVirtual).ToArray());

        _overlayProcessor.Write(
            locations
                .Where(x => x.IsVirtual)
                .Select(current =>
                {
                    var source = Locations.Lookup(current.TargetId!).Value;

                    var key = new VirtualLocationKey((VisioShapeId)current.ProxyGroupId!,
                        (VisioShapeId)current.TargetId!);
                    var overlay = _overlayProcessor.Cache.Lookup(key);

                    // 有四种情况：
                    // 1. 没有Overlay，值与source一样: 不操作
                    // 2. 没有Overlay，值与source不一样：新建Overlay并填入值
                    // 3. 有Overlay，值与source一样：删除Overlay 
                    // 4. 有Overlay，值与source不一样：覆盖值

                    var hasOverlay = overlay.HasValue;
                    var isValueEqual = !(Math.Abs(source.Quantity - current.Quantity) > FloatTolerance) &&
                                       source.Code == current.Code &&
                                       source.UnitMultiplier == current.UnitMultiplier;

                    if (!hasOverlay && isValueEqual)
                        return null;

                    var overlayValue = overlay.HasValue ? overlay.Value : new LocationOverlay(key);

                    overlayValue.Quantity =
                        Math.Abs(source.Quantity - current.Quantity) < FloatTolerance ? null : current.Quantity;
                    overlayValue.Code = source.Code == current.Code ? null : current.Code;
                    overlayValue.UnitMultiplier =
                        source.UnitMultiplier == current.UnitMultiplier ? null : current.UnitMultiplier;
                    return overlayValue;
                })
                .Where(x => x != null)
                .Cast<LocationOverlay>()
                .ToArray()
        );
    }
}