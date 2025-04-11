using System;
using System.IO;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text.Json;
using AE.PID.Client.Core;
using Microsoft.Win32;
using Splat;

namespace AE.PID.Client.Infrastructure;

public class ConfigurationService : DisposableBase, IConfigurationService
{
    private readonly BehaviorSubject<Configuration> _configurationSubject;
    private readonly IExportService _exportService;
    private readonly object _lock = new();

    private readonly Subject<(Expression<Func<Configuration, object>> PropertyExpression, object NewValue)>
        _updateSubject = new();

    public ConfigurationService(IExportService exportService, string companyName, string productName, string version)
    {
        this.Log().Info("Initializing configuration service...");

        _exportService = exportService;

        RuntimeConfiguration.CompanyName = companyName;
        RuntimeConfiguration.ProductName = productName;
        RuntimeConfiguration.Version = version;

        // 2025.3.12： 获取应用程序根目录
        RuntimeConfiguration.InstallationPath = AppDomain.CurrentDomain.BaseDirectory;

        // ensure app data folder exist
        RuntimeConfiguration.DataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), companyName,
            productName);
        if (!Directory.Exists(RuntimeConfiguration.DataPath))
            Directory.CreateDirectory(RuntimeConfiguration.DataPath);

        var configurationPath = Path.Combine(RuntimeConfiguration.DataPath, "config.json");

        _configurationSubject = new BehaviorSubject<Configuration>(Load(configurationPath));
        Configuration = _configurationSubject.AsObservable();

        // save the lasted configuration if it is inactive in 5 seconds
        var configurationSaver = _updateSubject
            .QuiescentBuffer(TimeSpan.FromSeconds(5), CurrentThreadScheduler.Instance)
            .Select(updates =>
            {
                var configuration = (Configuration)_configurationSubject.Value.Clone();

                foreach (var update in updates) configuration.UpdateValue( update.PropertyExpression, update.NewValue);

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
        UUID = SystemInfoHelper.GetMacAddresses()
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
        configuration.Server = "http://localhost:32768";
        configuration.UserId = "6470";
    }

    private void Save(Configuration config, string filePath)
    {
        lock (_lock)
        {
            try
            {
                _exportService.SaveAsJson(filePath, config);
                this.Log().Info($"Configuration saved at path {filePath}.");
            }
            catch (Exception ex)
            {
                this.Log().Error(ex, "Failed to save configuration.");
            }
        }
    }

    private string? GetBaseDirectory()
    {
        // 在DEBUG时，Visual Studio直接加载的是Build的路径，这个时候的AddIn名称是项目的名称；
        // 在安装后，则是获取Product Name。
#if DEBUG
        var addInName = Assembly.GetExecutingAssembly().GetName().Name;
#else
        var addInName = "AE.PID";
#endif
        var registryPath = $"HKEY_CURRENT_USER\\Software\\Microsoft\\Visio\\AddIns\\{addInName}";
        var manifest = Registry.GetValue(registryPath, "Manifest", null) as string;

        if (string.IsNullOrEmpty(manifest)) return null;

        // 解析路径（例如：file:///C:/路径/AddIn.vsto|vstolocal）
        if (manifest!.Contains("|")) manifest = manifest.Split('|')[0];

        var vstoPath = Uri.UnescapeDataString(new Uri(manifest).LocalPath);
        var baseDirectory = Path.GetDirectoryName(vstoPath)!;

        return baseDirectory;
    }
}