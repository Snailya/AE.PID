using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AE.PID.Controllers.Services;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.ViewModels.Pages;

public class UserSettingsViewModel : ViewModelBase
{
    private FrequencyOptionViewModel _libraryCheckFrequency;
    private FrequencyOptionViewModel _appNextCheckFrequency;
    private readonly ObservableCollectionExtended<LibraryInfoViewModel> _servers = [];
    private ObservableAsPropertyHelper<IEnumerable<LibraryInfoViewModel>> _libraries;

    #region Read-Write Properties

    public OkCancelFeedbackViewModel OkCancelFeedbackViewModel { get; private set; } = new();

    public FrequencyOptionViewModel AppNextCheckFrequency
    {
        get => _appNextCheckFrequency;
        private set
        {
            if (value != _appNextCheckFrequency)
                this.RaiseAndSetIfChanged(ref _appNextCheckFrequency, value);
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

    public IEnumerable<FrequencyOptionViewModel> CheckFrequencyOptions { get; } = FrequencyOptionViewModel.GetOptions();
    public string TmpPath { get; } = ThisAddIn.TmpFolder;
    public ReactiveCommand<Unit, Unit> CheckForAppUpdate { get; private set; }
    public ReactiveCommand<Unit, Unit> CheckForLibrariesUpdate { get; private set; }
    public ReactiveCommand<Unit, Unit> OpenTmp { get; private set; }
    public ReactiveCommand<Unit, Unit> ClearCache { get; private set; }

    #endregion

    #region Output Properties

    public IEnumerable<LibraryInfoViewModel> Libraries => _libraries.Value;

    #endregion

    protected override void SetupCommands()
    {
        CheckForAppUpdate = ReactiveCommand.Create(AppUpdater.Invoke);
        CheckForLibrariesUpdate = ReactiveCommand.Create(LibraryUpdater.Invoke);

        OpenTmp = ReactiveCommand.Create(OpenTmlFolder);
        ClearCache = ReactiveCommand.CreateRunInBackground(DeleteFilesInTmpFolder);

        OkCancelFeedbackViewModel.Ok = ReactiveCommand.Create(SaveChanges);
        OkCancelFeedbackViewModel.Cancel = ReactiveCommand.Create(() => { });
    }

    protected override void SetupSubscriptions(CompositeDisposable d)
    {
        var local = Globals.ThisAddIn.Configuration.ConfigurationSubject.Select(x =>
            new ObservableCollection<LibraryInfoViewModel>(x.LibraryConfiguration.Libraries.Select(i =>
                new LibraryInfoViewModel
                {
                    Name = i.Name,
                    LocalVersion = new Version(i.Version)
                })));
        var server = Observable.Return<ObservableCollectionExtended<LibraryInfoViewModel>>([])
            .Concat(_servers.ToObservableChangeSet().AutoRefresh().ToCollection());

        local
            .CombineLatest(server)
            .Select((data) =>
            {
                foreach (var item in data.First)
                    item.RemoteVersion = data.Second.SingleOrDefault(i => i.Name == item.Name)?.RemoteVersion;

                return data.First;
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.Libraries, out _libraries)
            .DisposeWith(d);
    }

    protected override void SetupStart()
    {
        LoadCheckIntervals();

        GetUpdateInfoAsync();
    }

    private FrequencyOptionViewModel GetMatchedOption(TimeSpan timeSpan)
    {
        return CheckFrequencyOptions
            .OrderBy(x => Math.Abs(timeSpan.Ticks - x.TimeSpan.Ticks))
            .First();
    }

    private void LoadCheckIntervals()
    {
        _appNextCheckFrequency =
            GetMatchedOption(Globals.ThisAddIn.Configuration.ConfigurationSubject.Value
                .CheckInterval);

        _libraryCheckFrequency = GetMatchedOption(Globals.ThisAddIn.Configuration
            .ConfigurationSubject.Value.LibraryConfiguration.CheckInterval);
    }

    private Task GetUpdateInfoAsync()
    {
        return Task.Run(async () =>
        {
            var servers = (await LibraryUpdater.GetLibraries()).Select(x => new LibraryInfoViewModel()
            {
                Name = x.Name,
                RemoteVersion = new Version(x.Version)
            });

            _servers.AddRange(servers);
        });
    }

    private void OpenTmlFolder()
    {
        Process.Start("explorer.exe", $"\"{TmpPath}\"");
    }

    private static void DeleteFilesInTmpFolder()
    {
        if (!Directory.Exists(ThisAddIn.TmpFolder)) return;

        var files = Directory.GetFiles(ThisAddIn.TmpFolder);
        foreach (var file in files)
            File.Delete(file);
    }

    private void SaveChanges()
    {
        if (Globals.ThisAddIn.Configuration.CheckInterval != _appNextCheckFrequency.TimeSpan)
        {
            Globals.ThisAddIn.Configuration.CheckInterval = _appNextCheckFrequency.TimeSpan;
            Globals.ThisAddIn.Configuration.NextTime = DateTime.Now + _appNextCheckFrequency.TimeSpan;
        }

        if (Globals.ThisAddIn.Configuration.LibraryConfiguration.CheckInterval != _libraryCheckFrequency.TimeSpan)
        {
            Globals.ThisAddIn.Configuration.LibraryConfiguration.CheckInterval = _libraryCheckFrequency.TimeSpan;
            Globals.ThisAddIn.Configuration.LibraryConfiguration.NextTime =
                DateTime.Now + _libraryCheckFrequency.TimeSpan;
        }

        Globals.ThisAddIn.Configuration.Save();
    }
}