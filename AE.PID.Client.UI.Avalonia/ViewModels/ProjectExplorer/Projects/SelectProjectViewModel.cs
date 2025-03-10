using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using AE.PID.Client.UI.Avalonia.Shared;
using DynamicData;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public class SelectProjectViewModel : WindowViewModelBase
{
    private readonly ReadOnlyObservableCollection<ProjectViewModel> _data;
    private readonly NotificationHelper _notificationHelper;

    private readonly IProjectService _projectService;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isBusy;
    private string? _searchText;
    private ProjectViewModel? _selectedProject;

    #region -- Commands --

    public ReactiveCommand<Unit, Unit> Cancel { get; }

    #endregion

    public PageNavigatorViewModel PageNavigator { get; } = new(1, 15);

    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    public string? SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public ReactiveCommand<Unit, ProjectViewModel?> Confirm { get; private set; }

    public ProjectViewModel? SelectedProject
    {
        get => _selectedProject;
        set => this.RaiseAndSetIfChanged(ref _selectedProject, value);
    }

    public ReadOnlyObservableCollection<ProjectViewModel> Data => _data;

    private async Task<IEnumerable<ProjectViewModel>> LoadAsync(string? searchText,
        PageRequest pageRequest)
    {
        IsBusy = true;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        var result = new List<ProjectViewModel>();

        try
        {
            var response = await _projectService.GetAllAsync(searchText!, pageRequest,
                cancellationToken);
            result.AddRange(response.Items.Select(project => new ProjectViewModel(project)));

            // updates the page navigator with the page info
            PageNavigator.Update(response);
        }
        catch (Exception e)
        {
            _notificationHelper.Error("加载项目列表失败", e.Message, NotificationHelper.Routes.SelectProject);
        }
        finally
        {
            IsBusy = false;
        }

        return result;
    }

    #region -- Constructor--

    public SelectProjectViewModel(NotificationHelper notificationHelper, IProjectService projectService) : base(
        notificationHelper, NotificationHelper.Routes.SelectProject)
    {
        _projectService = projectService;
        _notificationHelper = notificationHelper;

        #region -- Commands --

        Confirm = ReactiveCommand.Create(() => SelectedProject);
        Cancel = ReactiveCommand.Create(() => { });

        #endregion

        #region -- Subscriptions --

        this.WhenAnyValue(x => x.PageNavigator.CurrentPage, x => x.PageNavigator.PageSize,
                (page, size) => new PageRequest(page, size))
            .CombineLatest(this.WhenAnyValue(x => x.SearchText)
                .Throttle(TimeSpan.FromMilliseconds(400))
                .DistinctUntilChanged()
            )
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(x =>
                ObservableChangeSet.Create<ProjectViewModel>(async list =>
                {
                    var items = await LoadAsync(x.Second, x.First);
                    list.AddRange(items);
                    return () => { };
                }))
            // repeatedly reload data using Dynamic Data's switch operator which will clear previous data and add newly loaded data
            .Switch()
            .Bind(out _data)
            .Subscribe();

        #endregion
    }

    internal SelectProjectViewModel()
    {
        // Design
    }

    #endregion
}