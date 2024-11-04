using AE.PID.Visio.Core.Models;
using AE.PID.Visio.Core.Models.Projects;

namespace AE.PID.Visio.Core.Interfaces;

public interface IProjectStore : IStore
{
    /// <summary>
    ///     The observable for project of the document.
    /// </summary>
    public IObservable<Result<Project?>> Project { get; }
    
    /// <summary>
    /// Get the current project, used by the recommend service.
    /// </summary>
    /// <returns></returns>
    public Project? GetCurrentProject();

    /// <summary>
    ///     Update the project that assigned to the document.
    /// </summary>
    /// <param name="project"></param>
    /// <returns></returns>
    void Update(Project project);
}