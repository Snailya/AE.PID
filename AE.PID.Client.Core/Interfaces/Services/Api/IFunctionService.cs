using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AE.PID.Client.Core;

public interface IFunctionService
{
    /// <summary>
    ///     Get the functions under the specified zone from server, used for select the function zone or function group
    ///     information by user.
    /// </summary>
    /// <param name="projectId">The id of the project</param>
    /// <param name="zoneId">
    ///     If no zone id is specified, the function zone will be returned; if the zone id is specified, the
    ///     function groups will be returned
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NetworkNotValidException">There is a network error between server and local.</exception>
    Task<IEnumerable<Function>> GetFunctionsAsync(int projectId, int? zoneId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get the stand function groups from the server, used for select standard function group as input to function
    ///     location.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NetworkNotValidException">There is a network error between server and local.</exception>
    Task<IEnumerable<Function>> GetStandardFunctionGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sync the local functions to server.
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="functionId"></param>
    /// <param name="subFunctions"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NetworkNotValidException">There is a network error between server and local.</exception>
    Task SyncFunctionGroupsAsync(int projectId, int functionId, Function[] subFunctions,
        CancellationToken cancellationToken = default);

    // /// <summary>
    // ///     Get the function by its id
    // /// </summary>
    // /// <param name="id"></param>
    // /// <param name="cancellationToken"></param>
    // /// <returns></returns>
    // /// <exception cref="NetworkNotValidException">There is a network error between server and local.</exception>
    // Task<Function> GetFunctionByIdAsync(int id, int, CancellationToken cancellationToken = default);
    Task<Function?> GetFunctionById(int id);
}