using System.Threading.Tasks;
using AE.PID.Client.Core;

namespace AE.PID.Client.Infrastructure;

public class FunctionResolver(IFunctionService functionService, ILocalCacheService localCacheService)
    : IFunctionResolver
{
    public async Task<ResolveResult<Function?>> ResolvedAsync(int? id)
    {
        if (id is null or 0) return new ResolveResult<Function?>(null, DataSource.Api);

        try
        {
            var function = await functionService.GetFunctionById(id.Value);

            return new ResolveResult<Function?>(function, DataSource.Api);
        }
        catch (NetworkNotValidException _)
        {
            // if the network is invalid now, try to resolve it from local cache.
            // however, the local cache can miss that data, so if there is no record in the local cache, simply return null
            var cache = localCacheService.GetFunctionById(id.Value);

            return new ResolveResult<Function?>(cache, DataSource.LocalCache);
        }
    }
}