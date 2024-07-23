using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using AE.PID.Dtos;
using AE.PID.Properties;
using AE.PID.Services;
using AE.PID.Tools;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Splat;

namespace AE.PID.ViewModels;

public class SettingsPageViewModel(
    ConfigurationService? configuration = null,
    AppUpdater? appUpdater = null,
    LibraryUpdater? libraryUpdater = null)
    : ViewModelBase
{
    private readonly AppUpdater _appUpdater = appUpdater ?? Locator.Current.GetService<AppUpdater>()!;

    private readonly ConfigurationService _configuration =
        configuration ?? Locator.Current.GetService<ConfigurationService>()!;

    private readonly LibraryUpdater _libraryUpdater = libraryUpdater ?? Locator.Current.GetService<LibraryUpdater>()!;
    private readonly SourceCache<LibraryDto, int> _serverLibraries = new(t => t.Id);

    private FrequencyOptionViewModel _appCheckFrequency;

    private ReadOnlyObservableCollection<LibraryInfoViewModel> _libraries = new([]);

    private FrequencyOptionViewModel _libraryCheckFrequency;

    private string _server = string.Empty;
    private string _user = string.Empty;

    #region Output Properties

    public ReadOnlyObservableCollection<LibraryInfoViewModel> Libraries => _libraries;

    #endregion

    private static void OpenTmlFolder()
    {
        Process.Start("explorer.exe", $"\"{Constants.TmpFolder}\"");
    }

    private static void DeleteFilesInTmpFolder()
    {
        if (!Directory.Exists(Constants.TmpFolder)) return;

        var files = Directory.GetFiles(Constants.TmpFolder);
        foreach (var file in files)
            File.Delete(file);

        WindowManager.ShowDialog(Resources.MSG_delete_tmp_files_completed, MessageBoxButton.OK);
    }

    private void SaveChanges()
    {
        if (_configuration.Server != _server)
            _configuration.Server = _server;

        if (_configuration.UserId != _user)
            _configuration.UserId = _user;

        if (_configuration.AppCheckInterval != _appCheckFrequency.TimeSpan)
            _configuration.AppCheckInterval = _appCheckFrequency.TimeSpan;

        if (_configuration.LibraryCheckInterval != _libraryCheckFrequency.TimeSpan)
            _configuration.LibraryCheckInterval = _libraryCheckFrequency.TimeSpan;
    }

    #region Setup

    protected override void SetupCommands()
    {
        CheckForAppUpdate = ReactiveCommand.CreateFromTask(async () =>
        {
            var hasUpdate = await _appUpdater.CheckUpdateAsync();
            if (!hasUpdate) WindowManager.ShowDialog(Resources.MSG_no_valid_update, MessageBoxButton.OK);
        });
        CheckForLibrariesUpdate =
            ReactiveCommand.CreateRunInBackground(() => _libraryUpdater.ManuallyInvokeTrigger.OnNext(Unit.Default));

        OpenTmp = ReactiveCommand.Create(OpenTmlFolder);
        ClearCache = ReactiveCommand.CreateRunInBackground(DeleteFilesInTmpFolder);

        OkCancelFeedbackViewModel.Ok = ReactiveCommand.Create(SaveChanges);
        OkCancelFeedbackViewModel.Cancel = ReactiveCommand.Create(() => { });
    }

    protected override void SetupSubscriptions(CompositeDisposable d)
    {
        _configuration.Libraries
            .Connect()
            .FullJoin(
                _serverLibraries.Connect(),
                a => a.Id,
                (serverKey, local, server) =>
                {
                    var library = local.HasValue ? new LibraryInfoViewModel(local.Value) : new LibraryInfoViewModel();

                    if (server.HasValue)
                        library.RemoteVersion = server.Value.Version;

                    return library;
                })
            .RemoveKey()
            .Sort(SortExpressionComparer<LibraryInfoViewModel>.Ascending(t => t.Id))
            .ObserveOn(AppScheduler.UIScheduler)
            .Bind(out _libraries)
            .Subscribe()
            .DisposeWith(d);
    }

    protected override void SetupStart()
    {
        _appCheckFrequency = FrequencyOptionViewModel.GetMatchedOption(_configuration.AppCheckInterval);
        _libraryCheckFrequency = FrequencyOptionViewModel.GetMatchedOption(_configuration.LibraryCheckInterval);
        _server = _configuration.Server;
        _user = _configuration.UserId;

        Task.Run(async () =>
        {
            var libraries = await _libraryUpdater.GetLibraryInfos();
            _serverLibraries.AddOrUpdate(libraries);
        });
    }

    #endregion

    #region Read-Write Properties

    public string Server
    {
        get => _server;
        set => this.RaiseAndSetIfChanged(ref _server, value);
    }

    public string User
    {
        get => _user;
        set => this.RaiseAndSetIfChanged(ref _user, value);
    }

    public FrequencyOptionViewModel AppCheckFrequency
    {
        get => _appCheckFrequency;
        private set => this.RaiseAndSetIfChanged(ref _appCheckFrequency, value);
    }

    public FrequencyOptionViewModel LibraryCheckFrequency
    {
        get => _libraryCheckFrequency;
        private set => this.RaiseAndSetIfChanged(ref _libraryCheckFrequency, value);
    }

    #endregion

    #region Read-Only Properties

    public OkCancelFeedbackViewModel OkCancelFeedbackViewModel { get; } = new();
    public ReactiveCommand<Unit, Unit>? CheckForAppUpdate { get; private set; }
    public ReactiveCommand<Unit, Unit>? CheckForLibrariesUpdate { get; private set; }
    public ReactiveCommand<Unit, Unit>? OpenTmp { get; private set; }
    public ReactiveCommand<Unit, Unit>? ClearCache { get; private set; }

    #endregion
}