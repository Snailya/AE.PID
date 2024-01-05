using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using AE.PID.Controllers.Services;
using AE.PID.Models;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class UserSettingsViewModel : ReactiveObject
{
    private FrequencyOptionViewModel _libraryCheckFrequency;
    private FrequencyOptionViewModel _appNextCheckFrequency;
    private string _tmpPath = ThisAddIn.TmpFolder;
    private IEnumerable<LibraryViewModel> _libraries;

    public UserSettingsViewModel()
    {
        GetCurrentInterval();
        // LoadLibraries();

        CheckForAppUpdate = ReactiveCommand.Create(AppUpdater.Invoke);
        CheckForLibrariesUpdate = ReactiveCommand.Create(LibraryUpdater.Invoke);

        OpenTmp = ReactiveCommand.Create(() => { Process.Start("explorer.exe", $"\"{_tmpPath}\""); });
        ClearCache = ReactiveCommand.CreateRunInBackground(() =>
        {
            if (!Directory.Exists(ThisAddIn.TmpFolder)) return;

            var files = Directory.GetFiles(ThisAddIn.TmpFolder);
            foreach (var file in files)
                File.Delete(file);
        });
        Submit = ReactiveCommand.Create(() =>
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

            Configuration.Save(Globals.ThisAddIn.Configuration);
        });
        Cancel = ReactiveCommand.Create(() => { });
    }

    private async Task LoadLibraries()
    {
        var dtos = await LibraryUpdater.GetLibraries();

        Libraries = dtos.Select(x => new LibraryViewModel
        {
            Name = x.Name,
            LocalVersion = new Version(Globals.ThisAddIn.Configuration.LibraryConfiguration.Libraries
                .Single(i => i.Name == x.Name)
                .Version),
            RemoteVersion = new Version(x.Version)
        });
    }

    public List<FrequencyOptionViewModel> CheckFrequencyOptions { get; set; } =
    [
        new FrequencyOptionViewModel
        {
            Label = "每小时",
            TimeSpan = TimeSpan.FromHours(1)
        },
        new FrequencyOptionViewModel
        {
            Label = "每天",
            TimeSpan = TimeSpan.FromDays(1)
        },
        new FrequencyOptionViewModel
        {
            Label = "每周",
            TimeSpan = TimeSpan.FromDays(7)
        }
    ];

    public FrequencyOptionViewModel AppNextCheckFrequency
    {
        get => _appNextCheckFrequency;
        set
        {
            if (value != _appNextCheckFrequency)
                this.RaiseAndSetIfChanged(ref _appNextCheckFrequency, value);
        }
    }

    public string TmpPath
    {
        get => _tmpPath;
        set
        {
            if (value != _tmpPath)
                this.RaiseAndSetIfChanged(ref _tmpPath, value);
        }
    }

    public FrequencyOptionViewModel LibraryCheckFrequency
    {
        get => _libraryCheckFrequency;
        set
        {
            if (value != _libraryCheckFrequency)
                this.RaiseAndSetIfChanged(ref _libraryCheckFrequency, value);
        }
    }

    public IEnumerable<LibraryViewModel> Libraries
    {
        get => _libraries;
        set => this.RaiseAndSetIfChanged(ref _libraries, value);
    }

    public ReactiveCommand<Unit, Unit> CheckForAppUpdate { get; set; }
    public ReactiveCommand<Unit, Unit> CheckForLibrariesUpdate { get; }
    public ReactiveCommand<Unit, Unit> OpenTmp { get; set; }
    public ReactiveCommand<Unit, Unit> ClearCache { get; set; }
    public ReactiveCommand<Unit, Unit> Submit { get; }
    public ReactiveCommand<Unit, Unit> Cancel { get; set; }

    private void GetCurrentInterval()
    {
        _appNextCheckFrequency = CheckFrequencyOptions
            .OrderBy(x => Math.Abs(Globals.ThisAddIn.Configuration.CheckInterval.Ticks - x.TimeSpan.Ticks)).First();
        _libraryCheckFrequency = CheckFrequencyOptions.OrderBy(x =>
                Math.Abs(Globals.ThisAddIn.Configuration.LibraryConfiguration.CheckInterval.Ticks - x.TimeSpan.Ticks))
            .First();
    }
}