using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using AE.PID.Models;
using AE.PID.Tools;
using DynamicData;
using DynamicData.Binding;
using Newtonsoft.Json;
using ReactiveUI;
using Splat;

namespace AE.PID.Services;

public class ConfigurationService : ReactiveObject, IEnableLogger
{
    private readonly CompositeDisposable _cleanUp = [];
    private const string ConfigFileName = "ae-pid.json";
    private readonly object _lock = new();

    private TimeSpan _appCheckInterval;
    private DateTime _appNextTime;
    private TimeSpan _libraryCheckInterval;
    private DateTime _libraryNextTime;

    private readonly SourceCache<ReactiveLibrary, int> _libraries = new(t => t.Id);
    private readonly IObservableList<LibraryItem> _libraryItems;

    #region Constructors

    public ConfigurationService()
    {
        // create a derived list that expose the library items.
        // this list is used for getting the id of the items from library.
        // do not use BindTo but BindToObservableList to support AutoRefresh
        _libraries.Connect()
            .RemoveKey()
            .TransformMany(x => x.Items)
            .BindToObservableList(out _libraryItems)
            .Subscribe()
            .DisposeWith(_cleanUp);

        var configuration = Load();

        // setup app configuration
        _appNextTime = configuration.NextTime;
        _appCheckInterval = configuration.CheckInterval;

        // setup library configuration
        _libraryNextTime = configuration.LibraryConfiguration.NextTime;
        _libraryCheckInterval = configuration.LibraryConfiguration.CheckInterval;
        _libraries.AddOrUpdate(configuration.LibraryConfiguration.Libraries.Select(ReactiveLibrary.FromLibrary));

        // when any property changed, save the changes in 5 seconds to enhance performance
        this.ObservableForProperty(x => x.AppNextTime, skipInitial: true).Select(_ => Unit.Default)
            .Merge(this.ObservableForProperty(x => x.AppCheckInterval, skipInitial: true).Select(_ => Unit.Default))
            .Merge(this.ObservableForProperty(x => x.LibraryNextTime, skipInitial: true).Select(_ => Unit.Default))
            .Merge(this.ObservableForProperty(x => x.LibraryCheckInterval, skipInitial: true).Select(_ => Unit.Default))
            .Merge(_libraries.Connect().WhenPropertyChanged(x => x.Version).Select(_ => Unit.Default))
            .Quiescent(TimeSpan.FromSeconds(5), CurrentThreadScheduler.Instance)
            .Subscribe(_ => Save())
            .DisposeWith(_cleanUp);
    }

    #endregion

    #region Read-Write Properties

    public DateTime AppNextTime
    {
        get => _appNextTime;
        set => this.RaiseAndSetIfChanged(ref _appNextTime, value);
    }

    public TimeSpan AppCheckInterval
    {
        get => _appCheckInterval;
        set
        {
            // limits the minimal value to 1 hour
            if (value == TimeSpan.Zero)
            {
                this.Log().Warn("The minium check interval is 1 hour.");
                value = TimeSpan.FromHours(1);
            }

            this.RaiseAndSetIfChanged(ref _appCheckInterval, value);
        }
    }

    public DateTime LibraryNextTime
    {
        get => _libraryNextTime;
        set => this.RaiseAndSetIfChanged(ref _libraryNextTime, value);
    }

    public TimeSpan LibraryCheckInterval
    {
        get => _libraryCheckInterval;
        set
        {
            // limits the minimal value to 1 hour
            if (value == TimeSpan.Zero)
            {
                this.Log().Warn("The minium check interval is 1 hour.");
                value = TimeSpan.FromMinutes(1);
            }

            this.RaiseAndSetIfChanged(ref _libraryCheckInterval, value);
        }
    }

#if DEBUG
    public Uri Api { get; private set; } = new("http://localhost:32768");
#else
    public Uri Api { get; private set; } = new("http://172.18.128.104:32768");
#endif

    #endregion

    #region Output Properties

    public IObservableCache<ReactiveLibrary, int> Libraries => _libraries.AsObservableCache();
    public IObservableList<LibraryItem> LibraryItems => _libraryItems;

    #endregion

    /// <summary>
    ///     Add or update the libraries. this will update the source cache in the class.
    ///     As the source cache updated, it will trigger the Save event and persist to the local configuration file.
    /// </summary>
    /// <param name="libraries"></param>
    public void UpdateLibraries(IEnumerable<ReactiveLibrary> libraries)
    {
        _libraries.Edit(updater => { updater.AddOrUpdate(libraries); });
    }

    /// <summary>
    ///     Loads the configuration from file or create a new configuration if not exist.
    /// </summary>
    /// <returns>An Configuration object.</returns>
    private static Configuration Load()
    {
        try
        {
            if (File.Exists(GetPath()))
            {
                var configContent = File.ReadAllText(GetPath());

                if (!string.IsNullOrEmpty(configContent))
                {
                    var localConfig = JsonConvert.DeserializeObject<Configuration>(configContent);
                    if (localConfig != null) return localConfig;
                }
            }
        }
        catch (Exception exception)
        {
            LogHost.Default.Error(exception,
                 $"Failed to log config from {GetPath()}, a default configuration file is used instead.");
        }

        return new Configuration();
    }

    /// <summary>
    ///     Get the absolute path of the configuration file.
    /// </summary>
    /// <returns></returns>
    private static string GetPath()
    {
        return Path.Combine(Constants.AppDataFolder, ConfigFileName);
    }

    /// <summary>
    ///     Saves the Configuration object to file.
    /// </summary>
    private void Save()
    {
        lock (_lock)
        {
            try
            {
                // buildup a new configuration
                var configuration = new Configuration
                {
                    NextTime = AppNextTime,
                    CheckInterval = AppCheckInterval,
                    LibraryConfiguration = new LibraryConfiguration
                    {
                        NextTime = LibraryNextTime,
                        CheckInterval = LibraryCheckInterval,
                        Libraries = _libraries.Items.Select(x => new Library
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Version = x.Version,
                            Hash = x.Hash,
                            Path = x.Path,
                            Items = x.Items
                        })
                    }
                };

                // app config
                using var configFileStream = File.Open(GetPath(), FileMode.Create);
                using var configStreamWriter = new StreamWriter(configFileStream, Encoding.UTF8);
                var jsonString = JsonConvert.SerializeObject(configuration, Formatting.Indented);
                configStreamWriter.Write(jsonString);
                configStreamWriter.Flush();

                this.Log().Info($"Configuration saved successfully at path: '{GetPath()}'");
            }
            catch (Exception ex)
            {
                this.Log().Error(ex, "Failed to save configuration");
            }
        }
    }
}