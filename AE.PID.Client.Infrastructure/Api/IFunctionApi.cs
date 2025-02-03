using System.Collections.Generic;
using System.Threading.Tasks;
using AE.PID.Core.DTOs;
using Refit;

namespace AE.PID.Client.Infrastructure;

public interface IFunctionApi
{
    /// <summary>
    ///     Get the functions from the server.
    ///     If no project id specified, get the standard functions; If only project id specified, return the function zones of
    ///     the project,
    ///     If the function id (function zone id) is specified, return the function groups under the zone.
    /// </summary>
    /// <param name="projectId">The id of the project in PDMS.</param>
    /// <param name="functionId">The id of the function zone in PDMS.</param>
    /// <returns></returns>
    [Get("/api/v3/functions")]
    Task<IEnumerable<FunctionDto>> GetFunctionsAsync([Query] int? projectId = null, [Query] int? functionId = null);

    /// <summary>
    ///     Get the function info by specifying its id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [Get("/api/v3/functions/{id}")]
    Task<FunctionDto> GetFunctionByIdAsync(int id);

    /// <summary>
    ///     Synchronize the functions in the local to the remote server.
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="functionId"></param>
    /// <param name="subFunctions"></param>
    /// <returns></returns>
    [Post("/api/v3/functions")]
    Task<IEnumerable<FunctionDto>> SyncFunctions([Query] int projectId, [Query] int functionId,
        FunctionDto[] subFunctions);
}