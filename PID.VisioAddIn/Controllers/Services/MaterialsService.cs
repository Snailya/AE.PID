using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using AE.PID.Core.DTOs;
using AE.PID.Models.BOM;
using DynamicData;
using Newtonsoft.Json;

namespace AE.PID.Controllers.Services;

public class MaterialsService : IDisposable
{
    private const int PageSize = 20;

    private readonly SourceCache<MaterialCategoryDto, int> _categories = new(t => t.Id);
    private readonly IDisposable _cleanUp;

    private readonly HttpClient _client;
    private readonly SourceCache<LastUsedDesignMaterial, string> _lastUsed = new(t => t.Source.Code);

    private readonly SourceCache<MaterialsRequestResult, DesignMaterialsQueryTerms> _requestResults =
        new(t => t.QueryTerms);

    public MaterialsService(HttpClient client)
    {
        _client = client;

        // auto clear request cache after 1 hour
        _cleanUp = _requestResults
            .ExpireAfter(_ => TimeSpan.FromHours(1), (IScheduler?)null)
            .Subscribe();

        // initialize categories
        // todo: cache
        _ = InitializeCategories();
    }

    public IObservableCache<MaterialCategoryDto, int> Categories => _categories.AsObservableCache();
    public IObservableCache<LastUsedDesignMaterial, string> LastUsed => _lastUsed.AsObservableCache();

    public IObservableCache<MaterialsRequestResult, DesignMaterialsQueryTerms> Materials =>
        _requestResults.AsObservableCache();

    public void Dispose()
    {
        _cleanUp.Dispose();
    }

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

    private async Task InitializeCategories()
    {
        var response = await _client.GetStringAsync("categories");
        var categories = JsonConvert.DeserializeObject<IEnumerable<MaterialCategoryDto>>(response);

        if (categories != null)
            _categories.AddOrUpdate(categories);
    }
}

public class MaterialsRequestResult
{
    public IEnumerable<DesignMaterial> Materials;
    public DesignMaterialsQueryTerms QueryTerms;
}