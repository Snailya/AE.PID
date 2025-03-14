using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using AE.PID.Core;
using DynamicData;
using Refit;
using Splat;

namespace AE.PID.Client.Infrastructure;

/// <summary>
///     p.s. Service method only throws known exceptions.
/// </summary>
public class ProjectService(IApiFactory<IProjectApi> apiFactory)
    : IProjectService, IEnableLogger
{
    private readonly SourceCache<ProjectDto, int> _caches = new(t => t.Id);

    /// <inheritdoc />
    public async Task<Project> GetByIdAsync(int id)
    {
        // first, try to get it from memory
        var cache = _caches.Lookup(id);
        if (cache.HasValue) return cache.Value.ToProject();

        // if there is no memory cache, try to get it from the server
        try
        {
            var remote = await apiFactory.Api!.GetProjectByIdAsync(id);
            _caches.AddOrUpdate(remote);
            return remote.ToProject();
        }
        // if failed to get the project through api, it should notify the user
        catch (ApiException apiException) when (apiException.StatusCode is HttpStatusCode.NotFound
                                                    or HttpStatusCode.NoContent)
        {
            throw new ProjectNotValidException(id);
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
        catch (Exception e)
        {
            Debugger.Break();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Paged<Project>> GetAllAsync(string searchTerm, PageRequest pageRequest,
        CancellationToken token = default)
    {
        try
        {
            var result = await apiFactory.Api.GetProjectsAsync(searchTerm, pageRequest.Page, pageRequest.Size);

            return new Paged<Project>
            {
                Items = result.Items.Select(x => x.ToProject()),
                Page = result.Page,
                Pages = result.Pages,
                TotalSize = result.TotalSize,
                PageSize = result.PageSize
            };
        }
        catch (ApiException e)
        {
            this.Log().Error(e, $"Params: [{nameof(searchTerm)}: {searchTerm}, {nameof(pageRequest)}: {pageRequest}]");

            throw new NetworkNotValidException();
        }
        catch (HttpRequestException e)
        {
            this.Log().Error(e, $"Params: [{nameof(searchTerm)}: {searchTerm}, {nameof(pageRequest)}: {pageRequest}]");

            throw new NetworkNotValidException();
        }
    }
}