using System;
using System.Threading.Tasks;

namespace AE.PID.Client.Core;

public interface IProjectLocationStore : IStore
{
    /// <summary>
    ///     The observable of the project location with its lazy project resolver
    /// </summary>
    public IObservable<(ProjectLocation Location, Lazy<Task<ResolveResult<Project?>>> Project)> ProjectLocation { get; }

    /// <summary>
    ///     Update the project that assigned to the document.
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    void Update(ProjectLocation location);
}