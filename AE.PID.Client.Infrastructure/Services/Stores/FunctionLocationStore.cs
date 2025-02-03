using System;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using DynamicData;

namespace AE.PID.Client.Infrastructure;

public class FunctionLocationStore : DisposableBase, IFunctionLocationStore
{
    private readonly IDataProvider _dataProvider;
    private readonly ILocalCacheService _localCacheService;

    public FunctionLocationStore(IDataProvider dataProvider, IFunctionResolver resolver,
        ILocalCacheService localCacheService
    )
    {
        _dataProvider = dataProvider;
        _localCacheService = localCacheService;

        FunctionLocations = _dataProvider.FunctionLocations.Connect()
            .Transform(x =>
                new ValueTuple<FunctionLocation, Lazy<Task<ResolveResult<Function?>>>>(x,
                    new Lazy<Task<ResolveResult<Function?>>>(
                        () => resolver.ResolvedAsync(x.FunctionId))))
            .AsObservableCache();
    }

    /// <inheritdoc />
    public async void Save()
    {
        var SolutionXmlKey = "materials";

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
    public IObservableCache<(FunctionLocation Location, Lazy<Task<ResolveResult<Function?>>> Function), ICompoundKey>
        FunctionLocations { get; }

    public void Update(FunctionLocation[] locations)
    {
        _dataProvider.FunctionLocationsUpdater.OnNext(locations);
    }

    /// <inheritdoc />
    public FunctionLocation? Find(ICompoundKey id)
    {
        var location = FunctionLocations.Lookup(id);
        return location.HasValue ? location.Value.Location : null;
    }

    public void Load()
    {
        if (_dataProvider is ILazyLoad lazyLoad)
            lazyLoad.Load();
    }
}