using System;
using System.Threading.Tasks;
using AE.PID.Client.Core;

namespace AE.PID.Client.Infrastructure;

public class MaterialResolver(IMaterialService materialService, ILocalCacheService localCacheService)
    : IMaterialResolver
{
    public Task<ResolveResult<Material?>> ResolvedAsync(int? id)
    {
        throw new NotImplementedException();
    }

    public async Task<ResolveResult<Material?>> ResolvedAsync(string? code)
    {
        if (string.IsNullOrEmpty(code)) return new ResolveResult<Material?>(null, DataSource.Api);

        try
        {
            var material = await materialService.GetByCodeAsync(code);

            return new ResolveResult<Material?>(material, DataSource.Api);
        }
        catch (NetworkNotValidException _)
        {
            // if the network is invalid now, try to resolve it from local cache.
            // however, the local cache can miss that data, so if there is no record in the local cache, simply return null
            var cache = localCacheService.GetMaterialByCode(code);

            return new ResolveResult<Material?>(cache, DataSource.LocalCache);
        }
    }
}