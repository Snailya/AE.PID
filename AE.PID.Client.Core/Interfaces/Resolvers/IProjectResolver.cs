using System.Threading.Tasks;

namespace AE.PID.Client.Core;

public interface IProjectResolver
{
    Task<ResolveResult<Project?>> ResolvedAsync(int? id);
}