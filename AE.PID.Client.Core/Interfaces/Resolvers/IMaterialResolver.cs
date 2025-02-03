using System.Threading.Tasks;

namespace AE.PID.Client.Core;

public interface IMaterialResolver
{
    Task<ResolveResult<Material?>> ResolvedAsync(int? id);
    Task<ResolveResult<Material?>> ResolvedAsync(string? code);
}