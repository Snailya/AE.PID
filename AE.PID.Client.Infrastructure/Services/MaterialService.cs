using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using AE.PID.Core.DTOs;
using AE.PID.Core.Models;
using AE.PID.Visio.Shared;
using DynamicData;
using Refit;
using Splat;

namespace AE.PID.Client.Infrastructure;

/// <summary>
///     The material service is a wrapper for the backend api that related  to provide material information.
///     This service uses source cache to temporally store the searched result so that it could be quickly used by other
///     service that request the detail when providing only the code property.
///     When trying to get material by key, it should throw exception when there is no related data available in the
///     database.
///     All the methods here should only throw known exceptions.
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
            var result = await apiFactory.Api.GetMaterialsAsync(categoryId, s, pageRequest.Page, pageRequest.Size);
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
    public async Task<Material> GetByCodeAsync(string code, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(code)) throw new ArgumentNullException(nameof(code));

        var cache = _caches.Lookup(code);
        if (cache.HasValue) return await ToMaterial(cache.Value);

        // if there is no memory cache, try to get it from the server
        try
        {
            var remote = await apiFactory.Api.GetMaterialByCodeAsync(code);

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

    public async Task<IEnumerable<Recommendation<Material>>> GetRecommendationAsync(MaterialLocationContext context,
        CancellationToken token = default)
    {
        try
        {
            var collectionDto = await apiFactory.Api.GetRecommendedMaterialsAsync(context.ProjectId,
                context.FunctionZone, context.FunctionGroup, context.FunctionElement, context.MaterialLocationType);

            var recommendations = await Task.WhenAll(collectionDto.Items.Select(async x => new Recommendation<Material>
            {
                CollectionId = collectionDto.Id,
                Id = x.Id,
                Data = await ToMaterial(x.Material)
            }));

            return recommendations;
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

    public async Task FeedbackAsync(MaterialLocationContext context, int materialId, int? collectionId = null,
        int? recommendationId = null)
    {
        var dto = new UserMaterialSelectionFeedbackDto
        {
            MaterialId = materialId,
            MaterialLocationContext = context,
            RecommendationCollectionId = collectionId,
            SelectedRecommendationId = recommendationId
        };
        await apiFactory.Api.FeedbackMaterialSelectionsAsync([dto]);
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