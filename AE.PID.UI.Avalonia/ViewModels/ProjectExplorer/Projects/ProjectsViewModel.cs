using System;
using System.Reactive;
using System.Reactive.Linq;
using AE.PID.Client.Core;
using AE.PID.UI.Shared;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public class ProjectsViewModel : ViewModelBase
{
    private readonly ObservableAsPropertyHelper<ProjectViewModel> _project =
        ObservableAsPropertyHelper<ProjectViewModel>.Default();

    #region -- Interactions --

    public Interaction<SelectProjectViewModel?, ProjectViewModel?> ShowSelectProjectDialog { get; } = new();

    #endregion

    #region -- Commands --

    public ReactiveCommand<Unit, ProjectViewModel?> SelectProject { get; }

    #endregion

    #region -- Constructors --

    public ProjectsViewModel(NotificationHelper notificationHelper, IProjectService projectService,
        IProjectLocationStore projectLocationStore)
    {
        #region Commands

        SelectProject = ReactiveCommand.CreateFromTask<Unit, ProjectViewModel?>(async _ =>
        {
            var viewModel = new SelectProjectViewModel(notificationHelper, projectService);
            var dialogResult = await ShowSelectProjectDialog.Handle(viewModel);
            return dialogResult;
        });
        SelectProject.ThrownExceptions
            .Subscribe(v =>
            {
                notificationHelper.Error("选择项目失败", v.Message, NotificationHelper.Routes.ProjectExplorer);
            });

        #endregion

        #region Subscriptions

        projectLocationStore.ProjectLocation
            .SelectMany(x => x.Project.Value)
            .Do(x =>
            {
                // if there is null project with message
                if (x.Value == null && !string.IsNullOrEmpty(x.Message))
                    notificationHelper.Error("加载项目信息失败", x.Message,
                        NotificationHelper.Routes.ProjectExplorer);
            })
            .Select(x => new ProjectViewModel(x))
            .ToProperty(this, v => v.Project, out _project);

        // when there is any project selection build up a new project location and propagate back
        SelectProject.WhereNotNull()
            .WithLatestFrom(projectLocationStore.ProjectLocation.Select(x => x.Location),
                (project, location) => location with { ProjectId = project.Id })
            .Subscribe(projectLocationStore.Update);

        #endregion
    }

    public ProjectViewModel Project => _project.Value;

    internal ProjectsViewModel()
    {
        // Design
    }

    #endregion
}