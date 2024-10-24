using System.Threading.Tasks;
using AE.PID.Visio.Core.Exceptions;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;

namespace AE.PID.Visio.Shared.Services;

public class MaterialResolver(IMaterialService materialService, ILocalCacheService localCacheService)
    : IMaterialResolver
{
    public async Task<Resolved<Material>?> GetMaterialByCodeAsync(string code)
    {
        try
        {
            return new Resolved<Material>(await materialService.GetByCodeAsync(code), ResolveType.Network);
        }
        catch (NetworkNotValidException _)
        {
            // if the network is invalid now, try to resolve it from local cache.
            // however, the local cache can miss that data, so if there is no record in the local cache, simply return null
            var cache = localCacheService.GetMaterialByCode(code);
            return cache != null ? new Resolved<Material>(cache, ResolveType.Cache) : null;
        }
        // ReSharper disable once RedundantCatchClause
        catch (MaterialNotValidException _)
        {
            // if the network is valid but the code matches no data, throw the exception to let user know he or she has input the wrong value
            throw;
        }
    }
}