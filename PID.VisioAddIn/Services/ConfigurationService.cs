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
    private const string ConfigFileName = "ae-pid.json";
    private readonly CompositeDisposable _cleanUp = [];

    private readonly SourceCache<ReactiveLibrary, int> _libraries = new(t => t.Id);
    private readonly IObservableList<LibraryItem> _libraryItems;
    private readonly object _lock = new();

    private TimeSpan _appCheckInterval;
    private DateTime _appNextTime;
    private TimeSpan _libraryCheckInterval;
    private DateTime _libraryNextTime;
    private string _server;
    private string _userId;

    #region Constructors

    public ConfigurationService()
    {
        // create a derived list that exposes the library items.
        // this list is used for getting the id of the items from libraries.
        // do not use BindTo but BindToObservableList to support AutoRefresh
        _libraries.Connect()
            .RemoveKey()
            .TransformMany(x => x.Items)
            .BindToObservableList(out _libraryItems)
            .Subscribe()
            .DisposeWith(_cleanUp);

        var configuration = Load();

        // setup app configuration
        _server = configuration.Server;
        _userId = configuration.UserId;
        _appNextTime = configuration.NextTime;
        _appCheckInterval = configuration.CheckInterval;

        // setup library configuration
        _libraryNextTime = configuration.LibraryConfiguration.NextTime;
        _libraryCheckInterval = configuration.LibraryConfiguration.CheckInterval;
        _libraries.AddOrUpdate(configuration.LibraryConfiguration.Libraries.Select(ReactiveLibrary.FromLibrary));

        // when any property changed, save the changes in 5 seconds to enhance performance
        this.ObservableForProperty(x => x.Server).Select(_ => Unit.Default)
            .Merge(this.ObservableForProperty(x => x.UserId).Select(_ => Unit.Default))
            .Merge(this.ObservableForProperty(x => x.AppNextTime).Select(_ => Unit.Default))
            .Merge(this.ObservableForProperty(x => x.AppCheckInterval).Select(_ => Unit.Default))
            .Merge(this.ObservableForProperty(x => x.LibraryNextTime).Select(_ => Unit.Default))
            .Merge(this.ObservableForProperty(x => x.LibraryCheckInterval).Select(_ => Unit.Default))
            .Merge(_libraries.Connect().WhenPropertyChanged(x => x.Version).Select(_ => Unit.Default))
            .Quiescent(TimeSpan.FromSeconds(5), CurrentThreadScheduler.Instance)
            .Subscribe(_ => Save())
            .DisposeWith(_cleanUp);
    }

    #endregion

    /// <summary>
    ///     Add or update the libraries.
    ///     This will update the source cache in the class.
    ///     As the source cache is updated, it will trigger the Save event and persist to the local configuration file.
    /// </summary>
    /// <param name="libraries"></param>
    public void UpdateLibraries(IEnumerable<ReactiveLibrary> libraries)
    {
        _libraries.Edit(updater => { updater.AddOrUpdate(libraries); });
    }

    /// <summary>
    ///     Loads the configuration from file or create a new configuration if not exist.
    /// </summary>
    /// <returns>A Configuration object.</returns>
    private static Configuration Load()
    {
        try
        {
            LogHost.Default.Info($"Try load configuration from {GetPath()}...");

            if (File.Exists(GetPath()))
            {
                var configContent = File.ReadAllText(GetPath());

                if (!string.IsNullOrEmpty(configContent))
                {
                    var localConfig = JsonConvert.DeserializeObject<Configuration>(configContent);
                    if (localConfig != null)
                    {
                        LogHost.Default.Info("Configuration loaded.");
                        return localConfig;
                    }
                }
            }
        }
        catch (Exception exception)
        {
            LogHost.Default.Error(exception,
                $"Failed to load configuration from {GetPath()}, a default configuration file will be used instead.");
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
                    Server = Server,
                    UserId = UserId,
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

                this.Log().Info($"Configuration saved at path {GetPath()}.");
            }
            catch (Exception ex)
            {
                this.Log().Error(ex, "Failed to save configuration.");
            }
        }
    }

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
                this.Log().Warn("The minimum check interval is by hour.");
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
                this.Log().Warn("The minimum check interval is by hour.");
                value = TimeSpan.FromMinutes(1);
            }

            this.RaiseAndSetIfChanged(ref _libraryCheckInterval, value);
        }
    }

    public string Server
    {
        get => _server;
        set => this.RaiseAndSetIfChanged(ref _server, value);
    }

    public string UserId
    {
        get => _userId;
        set => this.RaiseAndSetIfChanged(ref _userId, value);
    }

    #endregion

    #region Output Properties

    public IObservableCache<ReactiveLibrary, int> Libraries => _libraries.AsObservableCache();
    public IObservableList<LibraryItem> LibraryItems => _libraryItems;

    #endregion
}