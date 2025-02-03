using System.Threading.Tasks;

namespace AE.PID.Client.Core;

public interface IFunctionResolver
{
    Task<ResolveResult<Function?>> ResolvedAsync(int? id);
}