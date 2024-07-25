using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AE.PID.Dtos;
using AE.PID.Models;
using DynamicData;
using Newtonsoft.Json;
using ReactiveUI;
using Splat;

namespace AE.PID.Services;

public class MaterialsService : IDisposable, IEnableLogger
{
    private const int PageSize = 20;

    private readonly SourceCache<MaterialCategoryDto, int> _categories = new(t => t.Id);
    private readonly CompositeDisposable _cleanUp = new();

    private readonly ApiClient _client;
    private readonly SourceCache<LastUsedDesignMaterial, string> _lastUsed = new(t => t.Source.MaterialNo);

    private readonly ReadOnlyObservableCollection<DesignMaterial> _materials;

    private readonly SourceCache<MaterialsRequestResult, DesignMaterialsQueryTerms> _requestResults =
        new(t => t.QueryTerms!);

    #region Constructors

    public MaterialsService(ApiClient? client = null)
    {
        _client = client ?? Locator.Current.GetService<ApiClient>()!;

        // make the request result auto clear after 1 hour to improve accuracy
        _requestResults
            .ExpireAfter(_ => TimeSpan.FromHours(1), scheduler: null)
            .Subscribe()
            .DisposeWith(_cleanUp);

        // initialize category items
        Observable.FromAsync(() => _client.GetStringAsync(CategoriesApi))
            .Select(JsonConvert.DeserializeObject<IEnumerable<MaterialCategoryDto>>)
            .WhereNotNull()
            .Subscribe(
                x => { _categories.AddOrUpdate(x); },
                error => { this.Log().Error($"Faile to get material categories: {error}"); })
            .DisposeWith(_cleanUp);

        // initialize category maps
        Observable.FromAsync(() => _client.GetStringAsync(CategoriesMapApi))
            .Select(JsonConvert.DeserializeObject<Dictionary<string, string[]>>)
            .WhereNotNull()
            .Subscribe(x => CategoryMap = x)
            .DisposeWith(_cleanUp);

        _requestResults.Connect()
            .RemoveKey()
            .TransformMany(x => x.Materials)
            .Bind(out _materials)
            .DisposeMany()
            .Subscribe()
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
    ///     Request server for the query items if it does not exist yet.
    /// </summary>
    /// <param name="query"></param>
    public async Task PopulateMaterials(DesignMaterialsQueryTerms query)
    {
        if (_requestResults.Lookup(query).HasValue) return;

        var response =
            await _client.GetStringAsync(
                MaterialsApi(query.CategoryId, query.PageNumber, PageSize));
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
        var lastUsed = _lastUsed.Items.SingleOrDefault(x => x.Source.MaterialNo == designMaterial.MaterialNo);
        if (lastUsed == null)
            lastUsed = new LastUsedDesignMaterial(designMaterial);
        else
            lastUsed.LastUsed = DateTime.Now;

        _lastUsed.AddOrUpdate(lastUsed);
    }

    #region Output Properties

    public IObservableCache<MaterialCategoryDto, int> Categories => _categories.AsObservableCache();
    public IObservableCache<LastUsedDesignMaterial, string> LastUsed => _lastUsed.AsObservableCache();

    public IObservableCache<MaterialsRequestResult, DesignMaterialsQueryTerms> MaterialsGroupByCategory =>
        _requestResults.AsObservableCache();

    public ReadOnlyObservableCollection<DesignMaterial> Materials => _materials;

    #endregion

    #region Api

    private static string CategoriesApi => "api/v2/categories";

    private static string CategoriesMapApi => "api/v2/categories/map";

    private static string MaterialsApi(int categoryId, int pageNumber, int pageSize)
    {
        return $"api/v2/materials?category={categoryId}&pageNo={pageNumber}&pageSize={pageSize}";
    }

    #endregion
}

public class MaterialsRequestResult
{
    public IEnumerable<DesignMaterial> Materials = [];
    public DesignMaterialsQueryTerms? QueryTerms;
}