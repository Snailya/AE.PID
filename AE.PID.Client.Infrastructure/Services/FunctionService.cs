using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using AE.PID.Core;
using AE.PID.Core.DTOs;
using DynamicData;
using Refit;
using Splat;

namespace AE.PID.Client.Infrastructure;

/// <summary>
///     p.s. Service method only throws known exceptions.
/// </summary>
public class FunctionService
    : DisposableBase, IFunctionService
{
    private readonly IApiFactory<IFunctionApi> _apiFactory;

    private readonly SourceCache<FunctionDto, ValueTuple<FunctionType, int>> _caches = new(t =>
        new ValueTuple<FunctionType, int>(t.FunctionType, t.Id));

    private readonly SourceCache<FunctionDto, int> _standardCaches = new(t => t.Id);

    public FunctionService(IApiFactory<IFunctionApi> apiFactory)
    {
        _apiFactory = apiFactory;

        var standardCacheClear = _standardCaches.ExpireAfter(_ => TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2))
            .Subscribe();
        CleanUp.Add(standardCacheClear);
    }

    /// <inheritdoc />
    public async Task SyncFunctionGroupsAsync(int projectId, int functionId, Function[] subFunctions,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _apiFactory.Api.SyncFunctions(projectId, functionId, subFunctions.Select(x =>
                new FunctionDto
                {
                    Id = x.Id,
                    FunctionType = FunctionType.FunctionGroup,
                    Code = x.Code,
                    Name = x.Name,
                    EnglishName = x.EnglishName,
                    Description = x.Description,
                    IsEnabled = x.IsEnabled
                }).ToArray());

            Debug.WriteLine(JsonSerializer.Serialize(result));
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.BadRequest)
        {
            this.Log().Error(e);
            throw new Exception($"API Error: {e.Message}");
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

    public Task<Function?> GetFunctionById(int id)
    {
        // todo: 或许需要project id
        throw new NotImplementedException();
    }

    // /// <inheritdoc />
    // public async Task<Function> GetFunctionByIdAsync(int id, CancellationToken cancellationToken = default)
    // {
    //     // first, try to get it from memory
    //     var cache = _caches.Lookup(new ValueTuple<FunctionType, int>(FunctionType.FunctionGroup, id));
    //     if (cache.HasValue) return cache.Value.ToFunction();
    //
    //     // if there is no memory cache, try to get it from the server
    //     try
    //     {
    //         var remote = await _apiFactory.Api.GetFunctionByIdAsync(id);
    //         _caches.AddOrUpdate(remote);
    //         return remote.ToFunction();
    //     }
    //     // if failed to get the project through api, it should notify the user
    //     catch (ApiException apiException) when (apiException.StatusCode is HttpStatusCode.NotFound
    //                                                 or HttpStatusCode.NoContent)
    //     {
    //         throw new FunctionNotValidException(id);
    //     }
    //     catch (ApiException e)
    //     {
    //         this.Log().Error(e, $"Params:[{nameof(id)}: {id}]");
    //         throw new NetworkNotValidException();
    //     }
    //     catch (HttpRequestException e)
    //     {
    //         this.Log().Error(e, $"Params:[{nameof(id)}: {id}]");
    //         throw new NetworkNotValidException();
    //     }
    // }

    /// <inheritdoc />
    public async Task<IEnumerable<Function>> GetStandardFunctionGroupsAsync(
        CancellationToken cancellationToken = default)
    {
        // if there is already cached standard functions, return directly
        if (_standardCaches.Count != 0) return _standardCaches.Items.Select(x => x.ToFunction());

        try
        {
            // if not, request api for it
            var result = (await _apiFactory.Api.GetFunctionsAsync()).ToList();
            // update the local source cache
            _standardCaches.AddOrUpdate(result);

            return _standardCaches.Items.Select(x => x.ToFunction());
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
    public async Task<IEnumerable<Function>> GetFunctionsAsync(int projectId, int? zoneId = null,
        CancellationToken token = default)
    {
        try
        {
            var result = (await _apiFactory.Api.GetFunctionsAsync(projectId, zoneId)).ToArray();

            // update the local source cache
            _caches.AddOrUpdate(result);

            return result.Select(x => x.ToFunction());
        }
        catch (ApiException e)
        {
            this.Log().Error(e,
                $"Params:[{nameof(projectId)}: {projectId}, {nameof(zoneId)}: {zoneId}]");
            throw new NetworkNotValidException();
        }
        catch (HttpRequestException e)
        {
            this.Log().Error(e,
                $"Params:[{nameof(projectId)}: {projectId}, {nameof(zoneId)}: {zoneId}]");
            throw new NetworkNotValidException();
        }
    }
}