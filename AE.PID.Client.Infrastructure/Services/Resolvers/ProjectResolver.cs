using System.Threading.Tasks;
using AE.PID.Client.Core;

namespace AE.PID.Client.Infrastructure;

public class ProjectResolver(IProjectService projectService, ILocalCacheService localCacheService) : IProjectResolver
{
    public async Task<ResolveResult<Project?>> ResolvedAsync(int? id)
    {
        if (id is null or 0) return new ResolveResult<Project?>(null, DataSource.Api);

        try
        {
            var project = await projectService.GetByIdAsync(id.Value);

            return new ResolveResult<Project?>(project, DataSource.Api);
        }
        catch (ProjectNotValidException e)
        {
            return new ResolveResult<Project?>(null, DataSource.Api) { Message = e.Message };
        }
        catch (NetworkNotValidException _)
        {
            // if the network is invalid now, try to resolve it from local cache.
            // however, the local cache can miss that data, so if there is no record in the local cache, simply return null
            var cache = localCacheService.GetProjectById(id.Value);

            return new ResolveResult<Project?>(cache, DataSource.LocalCache);
        }
    }
}