using System;
using System.IO;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text.Json;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using AE.PID.Visio.Shared.Extensions;
using Splat;
using Configuration = AE.PID.Visio.Core.Models.Configuration;

namespace AE.PID.Visio.Shared.Services;

public class ConfigurationService : DisposableBase, IConfigurationService
{
    private readonly BehaviorSubject<Configuration> _configurationSubject;
    private readonly object _lock = new();
    private readonly IStorageService _storageService;

    private readonly Subject<(Expression<Func<Configuration, object>> PropertyExpression, object NewValue)>
        _updateSubject = new();

    public ConfigurationService(IStorageService storageService, string productName, string version)
    {
        this.Log().Info("Initializing configuration service...");

        _storageService = storageService;

        RuntimeConfiguration.ProductName = productName;
        RuntimeConfiguration.Version = version;

        // ensure app data folder exist
        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            productName);
        if (!Directory.Exists(appDataFolder)) Directory.CreateDirectory(appDataFolder);

        var configurationPath = Path.Combine(appDataFolder, "config.json");

        _configurationSubject = new BehaviorSubject<Configuration>(Load(configurationPath));
        Configuration = _configurationSubject.AsObservable();

        // save the lasted configuration if it is inactive in 5 seconds
        var configurationSaver = _updateSubject
            .QuiescentBuffer(TimeSpan.FromSeconds(5), CurrentThreadScheduler.Instance)
            .Select(updates =>
            {
                var configuration = (Configuration)_configurationSubject.Value.Clone();

                foreach (var update in updates)
                {
                    var propertyInfo = (PropertyInfo)((MemberExpression)update.PropertyExpression.Body).Member;
                    propertyInfo.SetValue(configuration, update.NewValue);
                }

                return configuration;
            })
            .Subscribe(configuration =>
            {
                Save(configuration, configurationPath);
                _configurationSubject.OnNext(configuration);
            });

        CleanUp.Add(configurationSaver);
        CleanUp.Add(_updateSubject);
        CleanUp.Add(_configurationSubject);

        this.Log().Info("Configuration service initialized.");
    }

    /// <inheritdoc />
    public Configuration GetCurrentConfiguration()
    {
        return _configurationSubject.Value;
    }

    /// <inheritdoc />
    public RuntimeConfiguration RuntimeConfiguration { get; } = new()
    {
        UUID = SystemInfoHelper.GetUUID()
    };


    /// <inheritdoc />
    public IObservable<Configuration> Configuration { get; }

    /// <inheritdoc />
    public void UpdateProperty(Expression<Func<Configuration, object>> propertyExpression, object newValue)
    {
        _updateSubject.OnNext((propertyExpression, newValue));
    }

    private Configuration Load(string filePath)
    {
        try
        {
            this.Log().Info($"Try load configuration from {filePath}...");

            if (File.Exists(filePath))
            {
                var configContent = File.ReadAllText(filePath);

                if (!string.IsNullOrEmpty(configContent))
                {
                    var localConfig = JsonSerializer.Deserialize<Configuration>(configContent);
                    if (localConfig != null)
                    {
#if DEBUG
                        UpdateDebugConfiguration(localConfig);
#endif
                        this.Log().Info(
                            $"Configuration loaded. The backend is at {localConfig.Server}. The current user id is {localConfig.UserId}.");

                        return localConfig;
                    }
                }
            }
        }
        catch (Exception exception)
        {
            this.Log().Error(exception,
                $"Failed to load configuration from {filePath}, a default configuration file will be used instead.");
        }

        var configuration = new Configuration();
#if DEBUG
        UpdateDebugConfiguration(configuration);
#endif
        this.Log().Info(
            $"Configuration loaded. The backend is at {configuration.Server}. The current user id is {configuration.UserId}.");

        return configuration;
    }

    private static void UpdateDebugConfiguration(Configuration configuration)
    {
        // configuration.Server = "http://172.18.168.35:32769";
        configuration.Server = "http://localhost:32768";
        //configuration.UserId = "6470";
    }

    private void Save(Configuration config, string filePath)
    {
        lock (_lock)
        {
            try
            {
                _storageService.SaveAsJson(filePath, config);
                this.Log().Info($"Configuration saved at path {filePath}.");
            }
            catch (Exception ex)
            {
                this.Log().Error(ex, "Failed to save configuration.");
            }
        }
    }
}