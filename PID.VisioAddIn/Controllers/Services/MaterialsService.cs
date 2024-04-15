using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AE.PID.Core.DTOs;
using AE.PID.Models.BOM;
using DynamicData;
using Newtonsoft.Json;
using ReactiveUI;

namespace AE.PID.Controllers.Services;

public class MaterialsService : IDisposable
{
    private const int PageSize = 20;

    private readonly SourceCache<MaterialCategoryDto, int> _categories = new(t => t.Id);
    private readonly CompositeDisposable _cleanUp = new();

    private readonly HttpClient _client;
    private readonly SourceCache<LastUsedDesignMaterial, string> _lastUsed = new(t => t.Source.Code);

    private readonly SourceCache<MaterialsRequestResult, DesignMaterialsQueryTerms> _requestResults =
        new(t => t.QueryTerms!);

    #region Constructors

    public MaterialsService(HttpClient client)
    {
        _client = client;

        // make the request result auto clear after 1 hour to improve accuracy
        _requestResults
            .ExpireAfter(_ => TimeSpan.FromHours(1), (IScheduler?)null)
            .Subscribe()
            .DisposeWith(_cleanUp);

        // initialize category items
        Observable.FromAsync(() => client.GetStringAsync("categories"))
            .Select(JsonConvert.DeserializeObject<IEnumerable<MaterialCategoryDto>>)
            .WhereNotNull()
            .Subscribe(
                x => { _categories.AddOrUpdate(x); })
            .DisposeWith(_cleanUp);

        // initialize category maps
        Observable.FromAsync(() => client.GetStringAsync("categories/map"))
            .Select(JsonConvert.DeserializeObject<Dictionary<string, string[]>>)
            .WhereNotNull()
            .Subscribe(x => CategoryMap = x)
            .DisposeWith(_cleanUp);
    }

    #endregion

    #region Read-Only Properties

    public Dictionary<string, string[]> CategoryMap { get; private set; } = new();

    #endregion

    public void Dispose()
    {
        _cleanUp.Dispose();
    }

    /// <summary>
    ///     Request server for the query items if it is not exist yet.
    /// </summary>
    /// <param name="query"></param>
    public async Task PopulateMaterials(DesignMaterialsQueryTerms query)
    {
        if (_requestResults.Lookup(query).HasValue) return;

        var response =
            await _client.GetStringAsync(
                $"materials?category={query.CategoryId}&pageNo={query.PageNumber}&pageSize={PageSize}");
        if (string.IsNullOrEmpty(response)) return;

        var paged = JsonConvert.DeserializeObject<Paged<MaterialDto>>(response);
        if (paged?.Items == null || !paged.Items.Any()) return;

        var materials = paged.Items.Select(DesignMaterial.FromDTO);
        _requestResults.AddOrUpdate(new MaterialsRequestResult { Materials = materials, QueryTerms = query });
    }

    /// <summary>
    ///     If a design material already exists in last used, update its last used time.
    ///     Otherwise, add to lists.
    /// </summary>
    /// <param name="designMaterial"></param>
    /// <param name="elementName"></param>
    public void AddToLastUsed(DesignMaterial designMaterial, string elementName)
    {
        var lastUsed = _lastUsed.Items.SingleOrDefault(x => x.Source.Code == designMaterial.Code);
        if (lastUsed == null)
            lastUsed = new LastUsedDesignMaterial(designMaterial);
        else
            lastUsed.LastUsed = DateTime.Now;

        lastUsed.UsedBy.Add(elementName);
        _lastUsed.AddOrUpdate(lastUsed);
    }

    #region Output Properties

    public IObservableCache<MaterialCategoryDto, int> Categories => _categories.AsObservableCache();
    public IObservableCache<LastUsedDesignMaterial, string> LastUsed => _lastUsed.AsObservableCache();

    public IObservableCache<MaterialsRequestResult, DesignMaterialsQueryTerms> Materials =>
        _requestResults.AsObservableCache();

    #endregion
}

public class MaterialsRequestResult
{
    public IEnumerable<DesignMaterial> Materials = [];
    public DesignMaterialsQueryTerms? QueryTerms;
}