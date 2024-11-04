using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using AE.PID.Visio.Core.Exceptions;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using AE.PID.Visio.Core.Models.Projects;
using Splat;

namespace AE.PID.Visio.Shared.Services;

public class ProjectStore : DisposableBase, IProjectStore
{
    private const string SolutionXmlKey = "projects";
    private readonly ILocalCacheService _localCacheService;

    private readonly IProjectService _projectService;

    private readonly BehaviorSubject<Result<Project?>> _projectSubject = new(Result<Project?>.Success(null));
    private readonly IVisioService _visioService;

    public ProjectStore(IProjectService projectService, IVisioService visioService,
        ILocalCacheService localCacheService)
    {
        _projectService = projectService;
        _visioService = visioService;
        _localCacheService = localCacheService;

        // initialize the data
        _ = LoadInitialData();
    }

    public Project? GetCurrentProject()
    {
        return _projectSubject.Value.Value;
    }

    /// <inheritdoc />
    public void Update(Project project)
    {
        // save the project to document sheet
        _visioService.UpdateDocumentProperties([
            new ValuePatch(CellNameDict.ProjectId, project.Id, true),
            new ValuePatch(CellNameDict.ProjectCode, project.Code, true)
        ]);

        // propagate if it is a successful action
        _projectSubject.OnNext(Result<Project?>.Success(project));
    }

    /// <inheritdoc />
    public IObservable<Result<Project?>> Project => _projectSubject.AsObservable();

    #region -- IPersistData --

    public void Save()
    {
        var current = _projectSubject.Value.Value;
        if (current == null) return;

        // save the data to solution xml before dispose
        _localCacheService.PersistAsSolutionXml<Project, int>(SolutionXmlKey, [current], x => x.Id, true);
    }

    #endregion

    private async Task LoadInitialData()
    {
        // check the document sheet to see if there is a project assigned before, if there is no project property saved in document sheet, complete load
        if (_visioService.GetDocumentProperty(CellNameDict.ProjectId) is not { } idStr) return;

        // if there is a likeable project id property, try to parse it as int
        // if the parse failed, return the property value error result
        if (!double.TryParse(idStr, out var idDouble))
        {
            this.Log().Error($"Unable to parse User.ProjectId value {idStr} to int, failed to load project.");
            _projectSubject.OnNext(
                Result<Project?>.Failure(new InvalidShapeSheetPropertyValueException(CellNameDict.ProjectId, idStr)));
        }

        // if it does is an int value, 
        var id = (int)idDouble;

        try
        {
            var (project, exception) = await Resolve(id);
            _projectSubject.OnNext(Result<Project?>.Success(project, exception?.Message));
        }
        catch (ProjectNotValidException e)
        {
            _projectSubject.OnNext(Result<Project?>.Failure(e));
        }
        catch (Exception e)
        {
            this.Log().Error($"Unreachable code: {e.StackTrace}");
        }
    }

    private async Task<(Project, Exception?)> Resolve(int id)
    {
        try
        {
            var project = await _projectService.GetByIdAsync(id);
            return (project, null);
        }
        // if there is a network error, try to resolve the project from solution xml
        catch (NetworkNotValidException e)
        {
            var project = ResolveProjectFromSolutionXml(id);
            if (project != null) return (project, e);
        }

        throw new ProjectNotValidException(id,
            $"Unable to get the project with id {id} from neither server nor local xml.");

        Project? ResolveProjectFromSolutionXml(int x)
        {
            return _localCacheService.GetProjectById(id);
        }
    }
}