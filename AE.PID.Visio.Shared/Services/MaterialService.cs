using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AE.PID.Core.DTOs;
using AE.PID.Visio.Core.Exceptions;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using DynamicData;
using Refit;
using Splat;

namespace AE.PID.Visio.Shared.Services;

/// <summary>
///     p.s. Service method only throw known exceptions.
/// </summary>
public class MaterialService(IApiFactory<IMaterialApi> apiFactory)
    : IMaterialService, IEnableLogger
{
    private readonly SourceCache<MaterialDto, string> _caches = new(t => t.Code);

    private IEnumerable<MaterialCategory>? _categories;
    private Dictionary<string, string[]> _maps = new();

    /// <inheritdoc />
    public async Task<Dictionary<string, string[]>> GetCategoryMapAsync()
    {
        try
        {
            if (!_maps.Any())
                _maps = await apiFactory.Api!.GetCategoriesMapAsync();

            return _maps;
        }
        catch (ApiException e)
        {
            this.Log().Error(e);
            throw new NetworkNotValidException();
        }
        catch (HttpRequestException e)
        {
            this.Log().Error(e);
            throw new NetworkNotValidException();
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MaterialCategory>> GetCategoriesAsync()
    {
        try
        {
            _categories = (await apiFactory.Api!.GetCategoriesAsync()).Select(x => new MaterialCategory
            {
                Id = x.Id,
                ParentId = x.ParentId,
                Name = x.NodeName,
                Code = x.Code
            });
            return _categories;
        }
        catch (ApiException e)
        {
            this.Log().Error(e);
            throw new NetworkNotValidException();
        }
        catch (HttpRequestException e)
        {
            this.Log().Error(e);
            throw new NetworkNotValidException();
        }
    }

    /// <inheritdoc />
    public async Task<Paged<Material>> GetAsync(int? categoryId, PageRequest pageRequest,
        CancellationToken token = default)
    {
        try
        {
            var result =
                await apiFactory.Api!.GetMaterialsAsync(categoryId, string.Empty, pageRequest.Page, pageRequest.Size);
            _caches.AddOrUpdate(result.Items);

            var items = result.Items.Select(async x => await ToMaterial(x)).Select(x => x.Result).ToList();
            return new Paged<Material>
            {
                Items = items,
                Page = result.Page,
                Pages = result.Pages,
                TotalSize = result.TotalSize,
                PageSize = result.PageSize
            };
        }
        catch (ApiException e)
        {
            this.Log().Error(e);
            throw new NetworkNotValidException();
        }
        catch (HttpRequestException e)
        {
            this.Log().Error(e);
            throw new NetworkNotValidException();
        }
    }

    /// <inheritdoc />
    public async Task<Paged<Material>> SearchAsync(string s, int? categoryId, PageRequest pageRequest,
        CancellationToken token = default)
    {
        try
        {
            var result = await apiFactory.Api!.GetMaterialsAsync(categoryId, s, pageRequest.Page, pageRequest.Size);
            _caches.AddOrUpdate(result.Items);

            var items = result.Items.Select(async x => await ToMaterial(x)).Select(x => x.Result).ToList();
            return new Paged<Material>
            {
                Items = items,
                Page = result.Page,
                Pages = result.Pages,
                TotalSize = result.TotalSize,
                PageSize = result.PageSize
            };
        }
        catch (ApiException e)
        {
            this.Log().Error(e);
            throw new NetworkNotValidException();
        }
        catch (HttpRequestException e)
        {
            this.Log().Error(e);
            throw new NetworkNotValidException();
        }
    }

    /// <inheritdoc />
    public async Task<Material?> GetByCodeAsync(string code, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(code)) return null;

        var cache = _caches.Lookup(code);
        if (cache.HasValue) return await ToMaterial(cache.Value);

        // if there is no memory cache, try to get it from the server
        try
        {
            var remote = await apiFactory.Api!.GetMaterialByCodeAsync(code);

            if (remote == null) return null;

            _caches.AddOrUpdate(remote);
            return await ToMaterial(remote);
        }
        // if failed to get the project through api, it should notify the user
        catch (ApiException apiException) when (apiException.StatusCode is HttpStatusCode.NotFound
                                                    or HttpStatusCode.NoContent)
        {
            throw new MaterialNotValidException(code);
        }
        catch (ApiException e)
        {
            this.Log().Error(e);
            throw new NetworkNotValidException();
        }
        catch (HttpRequestException e)
        {
            this.Log().Error(e);
            throw new NetworkNotValidException();
        }
    }

    private async Task<Material> ToMaterial(MaterialDto dto)
    {
        if (_categories == null)
            await GetCategoriesAsync();

        return new Material
        {
            Id = dto.Id,
            Code = dto.Code,
            Name = dto.Name,
            Category = _categories!.Single(i => i.Id == dto.Categories.First()),
            Properties = dto.Properties.Select(i => new MaterialProperty
            {
                Name = i.Name,
                Value = i.Value
            }).ToArray(),
            Brand = dto.Brand,
            Specifications = dto.Specifications,
            Type = dto.Type,
            Unit = dto.Unit,
            Supplier = dto.Manufacturer,
            ManufacturerMaterialNumber = dto.ManufacturerMaterialNumber,
            TechnicalDataEnglish = dto.TechnicalDataEnglish,
            TechnicalData = dto.TechnicalData,
            Classification = dto.Classification,
            Attachment = dto.Attachment
        };
    }
}