using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AE.PID.Client.Core;

namespace AE.PID.Client.Infrastructure;

public class ProjectLocationStore : DisposableBase, IProjectLocationStore
{
    private readonly IDataProvider _dataProvider;
    private readonly ILocalCacheService _localCacheService;
    private readonly IProjectResolver _resolver;
    private int? _projectId;

    public ProjectLocationStore(IDataProvider dataProvider, IProjectResolver resolver,
        ILocalCacheService localCacheService)
    {
        _dataProvider = dataProvider;
        _resolver = resolver;
        _localCacheService = localCacheService;

        ProjectLocation = _dataProvider.ProjectLocation
            .Do(x => { _projectId = x.ProjectId; })
            .Select(x =>
                new ValueTuple<ProjectLocation, Lazy<Task<ResolveResult<Project?>>>>(x,
                    new Lazy<Task<ResolveResult<Project?>>>(
                        () => resolver.ResolvedAsync(x.ProjectId))))
            .Replay(1).RefCount();
    }

    public IObservable<(ProjectLocation Location, Lazy<Task<ResolveResult<Project?>>> Project)> ProjectLocation { get; }

    public void Update(ProjectLocation location)
    {
        _dataProvider.ProjectLocationUpdater.OnNext(location);
    }

    public void Save()
    {
        var project = _resolver.ResolvedAsync(_projectId).Result;
        if (project.ResolveFrom == DataSource.Api && project.Value != null)
            _localCacheService.Add(project.Value);
    }
}