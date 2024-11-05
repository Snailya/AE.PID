using System;
using System.Reactive;
using System.Reactive.Linq;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models.Projects;
using AE.PID.Visio.UI.Avalonia.Services;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class ProjectsViewModel : ViewModelBase
{
    private readonly ObservableAsPropertyHelper<string> _projectName;
    private ProjectViewModel? _project;

    #region -- Interactions --

    public Interaction<SelectProjectViewModel?, ProjectViewModel?> ShowSelectProjectDialog { get; } = new();

    #endregion

    public ProjectViewModel? Project
    {
        get => _project;
        private set => this.RaiseAndSetIfChanged(ref _project, value);
    }

    #region -- Commands --

    public ReactiveCommand<Unit, Unit> SelectProject { get; }

    #endregion

    public string ProjectName => _projectName.Value;

    #region -- Constructors --

    public ProjectsViewModel(NotificationHelper notificationHelper, IProjectService projectService, IProjectStore projectStore)
    {
        #region Commands

        SelectProject = ReactiveCommand.CreateFromTask(async () =>
        {
            var viewModel = new SelectProjectViewModel(notificationHelper, projectService);
            var dialogResult = await ShowSelectProjectDialog.Handle(viewModel);
            if (dialogResult == null) return;

            // do update
            var project = new Project { Id = dialogResult.Id, Name = dialogResult.Name, Code = dialogResult.Code };
            projectStore.Update(project);
        });
        SelectProject.ThrownExceptions
            .Subscribe(v => { notificationHelper.Error("选择项目失败", v.Message, NotificationHelper.Routes.ProjectExplorer); });

        #endregion

        #region Subscriptions

        projectStore
            .Project
            .Do(x =>
            {
                if (!x.IsSuccess)
                    notificationHelper.Error("加载项目信息失败", x.Exception!.Message, NotificationHelper.Routes.ProjectExplorer);
                else if (!string.IsNullOrEmpty(x.Message))
                    notificationHelper.Warning(message: x.Message, route: NotificationHelper.Routes.ProjectExplorer);
            })
            .Select(x => x.Value)
            .Select(x => x == null ? null : new ProjectViewModel(x))
            .Subscribe(v => { Project = v; });

        this.WhenAnyValue(x => x.Project)
            .Select(x => x?.Name ?? string.Empty)
            .ToProperty(this, v => v.ProjectName, out _projectName);

        #endregion
    }


    internal ProjectsViewModel()
    {
        // Design
    }

    #endregion
}