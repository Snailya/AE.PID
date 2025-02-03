using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using AE.PID.Client.Core;
using AE.PID.Core.Models;
using AE.PID.Visio.UI.Design.Design;
using DynamicData;

namespace AE.PID.Visio.UI.Design;

internal sealed class MoqDataProvider : IDataProvider, IDisposable
{
    private readonly CompositeDisposable _cleanUp = new();

    private readonly SourceCache<FunctionLocation, ICompoundKey> _functionLocations = new(t => t.Id);

    private readonly SourceCache<MaterialLocation, ICompoundKey> _materialLocations = new(t => t.Id);

    private readonly BehaviorSubject<ProjectLocation> _projectLocationSubject =
        new(new ProjectLocation(new LocationKey(1), 1));

    public MoqDataProvider()
    {
        // 发射流
        ProjectLocation = _projectLocationSubject;
        FunctionLocations = _functionLocations;
        MaterialLocations = _materialLocations;

        // 返回流
        ProjectLocationUpdater.Subscribe(x => _projectLocationSubject.OnNext(x)).DisposeWith(_cleanUp);
        MaterialLocationsUpdater.Subscribe(x => _materialLocations.AddOrUpdate(x)).DisposeWith(_cleanUp);
        FunctionLocationsUpdater.Subscribe(x => _functionLocations.AddOrUpdate(x)).DisposeWith(_cleanUp);

        // 初始化数据
        _functionLocations.AddOrUpdate(DesignData.Shapes.Select(ToFunctionLocation));
        _materialLocations.AddOrUpdate(DesignData.Shapes.Where(x => x.ShapeCategory == "Equipment")
            .Select(ToMaterialLocation));
    }


    public IObservable<ProjectLocation> ProjectLocation { get; }
    public Subject<ProjectLocation> ProjectLocationUpdater { get; } = new();
    public IObservableCache<FunctionLocation, ICompoundKey> FunctionLocations { get; }

    public Subject<FunctionLocation[]> FunctionLocationsUpdater { get; } = new();
    public IObservableCache<MaterialLocation, ICompoundKey> MaterialLocations { get; }
    public Subject<MaterialLocation[]> MaterialLocationsUpdater { get; } = new();

    public ICompoundKey[] GetAdjacent(ICompoundKey compositeId)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        _cleanUp.Dispose();
    }

    private static FunctionLocation ToFunctionLocation(ShapeProxy shape)
    {
        var id = new LocationKey(shape.Id);
        var parentId = new LocationKey(shape.ParentId);
        var type = ResolveFunctionType(shape);

        return new FunctionLocation(id, parentId, string.Empty, type, null, shape.Zone, shape.ZoneName,
            shape.ZoneNameEnglish,
            shape.Group, shape.GroupName, shape.GroupNameEnglish, shape.Element, shape.Description, shape.Remarks,
            string.Empty);
    }

    private static FunctionType ResolveFunctionType(ShapeProxy source)
    {
        return source.ShapeCategory switch
        {
            "Frame" => FunctionType.ProcessZone,
            "FunctionalGroup" => FunctionType.FunctionGroup,
            "Equipment" => FunctionType.Equipment,
            _ => throw new ArgumentOutOfRangeException(nameof(source.ShapeCategory))
        };
    }

    private static MaterialLocation ToMaterialLocation(ShapeProxy shape)
    {
        return new MaterialLocation(new LocationKey(shape.Id), shape.MaterialCode, 1, 1, "", shape.MaterialType);
    }
}