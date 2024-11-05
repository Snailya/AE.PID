using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using AE.PID.Core.Models;
using AE.PID.Visio.Core.Exceptions;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using DynamicData;

namespace AE.PID.Visio.Shared.Services;

public class FunctionLocationStore : DisposableBase, IFunctionLocationStore
{
    private const string SolutionXmlKey = "materials";
    private readonly SourceCache<FunctionLocation, CompositeId> _functionLocations = new(t => t.Id);
    private readonly Lazy<IDisposable> _loader;

    private readonly IVisioService _visioService;

    public FunctionLocationStore(IFunctionService functionService,
        IVisioService visioService)
    {
        _visioService = visioService;
        
        FunctionLocations = _functionLocations.AsObservableCache(); 

        // the function locations is implemented as lazy load by invoke the Load method, because for a drawing there might be hundreds of material locations which takes a lot of time to load  
        // we should also consider to implement the load method into an async method with cancellation
        _loader = new Lazy<IDisposable>(() => visioService.Shapes.Value
            .Connect()
            .Filter(x => x.Types.Contains(VisioShape.ShapeType.FunctionLocation))
            .Transform(visioService.ToFunctionLocation)
            // after the shape is transformed into the model, switch the forward working into background scheduler
            .ObserveOn(TaskPoolScheduler.Default)
            .PopulateInto(_functionLocations)
        );

        CleanUp.Add(Disposable.Create(() =>
        {
            if (_loader.IsValueCreated)
                _loader.Value.Dispose();
        }));

        // observable property changes and propagate to Visio shape
        var propagateChangeToVisio = FunctionLocations.Connect()
            .AutoRefresh(propertyChangeThrottle: TimeSpan.FromMilliseconds(400))
            .WhereReasonsAre(ChangeReason.Refresh)
            .Subscribe(changes =>
            {
                foreach (var change in changes) UpdateInVisio(change);
            });

        CleanUp.Add(propagateChangeToVisio);
    }

    /// <inheritdoc />
    public async void Save()
    {
        // // save the funcdtion zone inf
        // var tasks = FunctionLocations.Items.Where(x => x.Type== FunctionType.FunctionGroup && x.FunctionId != default)
        //     .Select(async x => await _functionService.GetFunctionByIdAsync(x.FunctionId)).ToList();
        //
        // var functions = (await Task.WhenAll(tasks)).Where(x => x != null).Select(x => x)
        //     .ToArray();
        //
        // if (!functions.Any()) return;
        //
        // // save the functions to solution xml
        // if (functions.Any())
        //     _visioService.PersistAsSolutionXml(SolutionXmlKey, functions, x => x.Id);
    }

    /// <inheritdoc />
    public IObservableCache<FunctionLocation, CompositeId> FunctionLocations { get; } 

    /// <inheritdoc />
    public void Update(CompositeId id, Function function)
    {
        var location = FunctionLocations.Lookup(id);
        if (!location.HasValue) throw new FunctionLocationNotValidException(id);

        if (function.Type != location.Value.Type)
            throw new FunctionTypeNotMatchException(function.Type, location.Value.Type);

        switch (location.Value.Type)
        {
            case FunctionType.ProcessZone:
                location.Value.FunctionId = function.Id;
                location.Value.Zone = function.Code;
                location.Value.ZoneName = function.Name;
                location.Value.ZoneEnglishName = function.EnglishName;
                location.Value.Description = function.Description;
                break;
            case FunctionType.FunctionGroup:
                location.Value.FunctionId = function.Id;

                // there are two circumstance, the first is that the function group field is totally empty, then use the default function group code as input
                if (string.IsNullOrEmpty(location.Value.Group))
                {
                    location.Value.Group = function.Code;
                }
                else // the second circumstance is that the user has already specified a number for the group, then strip it from the field and replace the prefix only
                {
                    var number = Regex.Match(location.Value.Group, @"(\d+)$").Value;
                    var prefix = Regex.Match(function.Code, @"(^[A-Za-z]+)").Value;
                    location.Value.Group = prefix + number;
                }

                location.Value.GroupName = function.Name;
                location.Value.GroupEnglishName = function.EnglishName;
                break;
        }
    }

    /// <inheritdoc />
    public FunctionLocation? Find(CompositeId id)
    {
        var location = FunctionLocations.Lookup(id);
        return location.HasValue ? location.Value : null;
    }

    public void Load()
    {
        var _ = _loader.Value;
    }

    private void UpdateInVisio(Change<FunctionLocation, CompositeId> change)
    {
        var patches = new List<ValuePatch>();

        switch (change.Current.Type)
        {
            case FunctionType.Equipment or FunctionType.Instrument
                or FunctionType.FunctionElement:
                var value = Regex.Match(change.Current.Element, @"\d+").Value;
                patches.AddRange([
                    new ValuePatch(CellNameDict.FunctionElement, value),
                    new ValuePatch(CellNameDict.Description, change.Current.Description)
                ]);
                break;
            case FunctionType.FunctionGroup:
                patches.AddRange([
                    new ValuePatch(CellNameDict.FunctionGroup, change.Current.Group),
                    new ValuePatch(CellNameDict.FunctionGroupName, change.Current.GroupName),
                    new ValuePatch(CellNameDict.FunctionGroupEnglishName, change.Current.GroupEnglishName),
                    new ValuePatch(CellNameDict.FunctionGroupDescription, change.Current.Description)
                ]);

                break;
            case FunctionType.ProcessZone:
                patches.AddRange([
                    new ValuePatch(CellNameDict.FunctionZone, change.Current.Zone),
                    new ValuePatch(CellNameDict.FunctionZoneName, change.Current.ZoneName),
                    new ValuePatch(CellNameDict.FunctionZoneEnglishName, change.Current.ZoneEnglishName)
                ]);

                break;
        }

        patches.Add(new ValuePatch(CellNameDict.FunctionId, change.Current.FunctionId, true));
        patches.Add(new ValuePatch(CellNameDict.Remarks, change.Current.Remarks, true));

        _visioService.UpdateShapeProperties(change.Key, patches);
    }
}