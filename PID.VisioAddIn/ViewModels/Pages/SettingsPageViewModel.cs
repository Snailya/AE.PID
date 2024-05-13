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
using AE.PID.Services;
using AE.PID.Tools;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class SettingsPageViewModel(
    ConfigurationService configuration,
    AppUpdater appUpdater,
    LibraryUpdater libraryUpdater)
    : ViewModelBase
{
    private readonly SourceCache<LibraryDto, int> _serverLibraries = new(t => t.Id);

    private FrequencyOptionViewModel _appCheckFrequency =
        FrequencyOptionViewModel.GetMatchedOption(configuration.AppCheckInterval);

    private ReadOnlyObservableCollection<LibraryInfoViewModel> _libraries = new([]);

    private FrequencyOptionViewModel _libraryCheckFrequency =
        FrequencyOptionViewModel.GetMatchedOption(configuration.LibraryCheckInterval);

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

        WindowManager.ShowDialog("清除成功", MessageBoxButton.OK);
    }

    private void SaveChanges()
    {
        if (configuration.AppCheckInterval != _appCheckFrequency.TimeSpan)
            configuration.AppCheckInterval = _appCheckFrequency.TimeSpan;

        if (configuration.LibraryCheckInterval != _libraryCheckFrequency.TimeSpan)
            configuration.LibraryCheckInterval = _libraryCheckFrequency.TimeSpan;
    }

    #region Setup

    protected override void SetupCommands()
    {
        CheckForAppUpdate = ReactiveCommand.CreateFromTask(async () =>
        {
            var hasUpdate = await appUpdater.CheckUpdateAsync();
            if (!hasUpdate) WindowManager.ShowDialog("已经是最新版", MessageBoxButton.OK);
        });
        CheckForLibrariesUpdate =
            ReactiveCommand.CreateRunInBackground(() => libraryUpdater.ManuallyInvokeTrigger.OnNext(Unit.Default));

        OpenTmp = ReactiveCommand.Create(OpenTmlFolder);
        ClearCache = ReactiveCommand.CreateRunInBackground(DeleteFilesInTmpFolder);

        OkCancelFeedbackViewModel.Ok = ReactiveCommand.Create(SaveChanges);
        OkCancelFeedbackViewModel.Cancel = ReactiveCommand.Create(() => { });
    }

    protected override void SetupSubscriptions(CompositeDisposable d)
    {
        configuration.Libraries.Connect().FullJoin(
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
            .ObserveOn(WindowManager.Dispatcher!)
            .Bind(out _libraries)
            .Subscribe()
            .DisposeWith(d);
    }

    protected override void SetupStart()
    {
        Task.Run(async () =>
        {
            var libraries = await libraryUpdater.GetLibraryInfos();
            _serverLibraries.AddOrUpdate(libraries);
        });
    }

    #endregion

    #region Read-Write Properties

    public FrequencyOptionViewModel AppCheckFrequency
    {
        get => _appCheckFrequency;
        private set
        {
            if (value != _appCheckFrequency)
                this.RaiseAndSetIfChanged(ref _appCheckFrequency, value);
        }
    }

    public FrequencyOptionViewModel LibraryCheckFrequency
    {
        get => _libraryCheckFrequency;
        private set
        {
            if (value != _libraryCheckFrequency)
                this.RaiseAndSetIfChanged(ref _libraryCheckFrequency, value);
        }
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