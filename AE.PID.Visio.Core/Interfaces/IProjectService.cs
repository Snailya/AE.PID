using AE.PID.Core.DTOs;
using AE.PID.Visio.Core.Exceptions;
using AE.PID.Visio.Core.Models.Projects;
using DynamicData;

namespace AE.PID.Visio.Core.Interfaces;

public interface IProjectService
{
    /// <summary>
    ///     Get the projects from the server.
    /// </summary>
    /// <param name="searchTerm"></param>
    /// <param name="pageRequest"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="NetworkNotValidException">There is a network error between server and local.</exception>
    Task<Paged<Project>> GetAllAsync(string searchTerm, PageRequest pageRequest,
        CancellationToken token = default);

    /// <summary>
    ///     Get the project by its id.
    /// </summary>
    /// <param name="id">The id for project in PDMS database</param>
    /// <returns></returns>
    /// <exception cref="ProjectNotValidException">There is no valid project with the id in database.</exception>
    /// <exception cref="NetworkNotValidException">There is a network error between server and local.</exception>
    Task<Project> GetByIdAsync(int id);
}