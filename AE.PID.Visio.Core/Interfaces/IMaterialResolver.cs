using AE.PID.Visio.Core.Exceptions;
using AE.PID.Visio.Core.Models;

namespace AE.PID.Visio.Core.Interfaces;

public interface IMaterialResolver
{
    /// <summary>
    ///     This method try to resolve the code to a material by firstly from web api, fallback to local cache if failed.
    /// </summary>
    /// <param name="code">The user oriented id</param>
    /// <returns>The material that matches the code, null if it is unable to determine now.</returns>
    /// <exception cref="MaterialNotValidException">There is no valid material in the database that matches the key.</exception>
    Task<Resolved<Material>?> GetMaterialByCodeAsync(string code);
}

