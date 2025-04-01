using System.Threading;
using System.Threading.Tasks;
using AE.PID.Core;

namespace AE.PID.Client.Core;

public interface IProjectService
{
    /// <summary>
    ///     Get the projects from the server.
    /// </summary>
    /// <param name="searchTerm"></param>
    /// <param name="size"></param>
    /// <param name="token"></param>
    /// <param name="page"></param>
    /// <returns></returns>
    /// <exception cref="NetworkNotValidException">There is a network error between server and local.</exception>
    Task<Paged<Project>> GetAllAsync(string searchTerm, int page, int size,
        CancellationToken token = default);

    /// <summary>
    ///     Get the project by its id.
    /// </summary>
    /// <param name="id">The id for a project in PDMS database</param>
    /// <returns></returns>
    /// <exception cref="ProjectNotValidException">There is no valid project with the id in database.</exception>
    /// <exception cref="NetworkNotValidException">There is a network error between server and local.</exception>
    Task<Project> GetByIdAsync(int id);
}