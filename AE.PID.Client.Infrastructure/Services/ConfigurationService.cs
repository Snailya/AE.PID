using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text.Json;
using AE.PID.Client.Core;
using Splat;

namespace AE.PID.Client.Infrastructure;

public class ConfigurationService : DisposableBase, IConfigurationService
{
    private readonly BehaviorSubject<Configuration> _configurationSubject;
    private readonly object _lock = new();
    private readonly IStorageService _storageService;

    private readonly Subject<(Expression<Func<Configuration, object>> PropertyExpression, object NewValue)>
        _updateSubject = new();

    public ConfigurationService(IStorageService storageService, string companyName, string productName, string version)
    {
        this.Log().Info("Initializing configuration service...");

        _storageService = storageService;

        RuntimeConfiguration.CompanyName = companyName;
        RuntimeConfiguration.ProductName = productName;
        RuntimeConfiguration.Version = version;

        // ensure app data folder exist
        RuntimeConfiguration.AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), companyName,
            productName);
        if (!Directory.Exists(RuntimeConfiguration.AppDataFolder))
            Directory.CreateDirectory(RuntimeConfiguration.AppDataFolder);

        var configurationPath = Path.Combine(RuntimeConfiguration.AppDataFolder, "config.json");

        _configurationSubject = new BehaviorSubject<Configuration>(Load(configurationPath));
        Configuration = _configurationSubject.AsObservable();

        // save the lasted configuration if it is inactive in 5 seconds
        var configurationSaver = _updateSubject
            .QuiescentBuffer(TimeSpan.FromSeconds(5), CurrentThreadScheduler.Instance)
            .Select(updates =>
            {
                var configuration = (Configuration)_configurationSubject.Value.Clone();

                foreach (var update in updates) UpdateValue(configuration, update.PropertyExpression, update.NewValue);

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

    private static void UpdateValue<T, TValue>(T target, Expression<Func<T, TValue>> memberExpression, TValue newValue)
    {
        if (memberExpression.Body is MemberExpression memberExpr)
        {
            // Traverse to the final object and member
            var (finalTarget, member) = GetFinalTargetAndMember(target, memberExpr);

            switch (member)
            {
                // Update the property value
                case PropertyInfo property:
                    property.SetValue(finalTarget, newValue);
                    break;
                // Update the field value
                case FieldInfo field:
                    field.SetValue(finalTarget, newValue);
                    break;
                default:
                    throw new InvalidOperationException("MemberExpression must target a property or field.");
            }
        }
        else
        {
            throw new InvalidOperationException("Expression must be a MemberExpression.");
        }
    }

    private static (object? FinalTarget, MemberInfo Member) GetFinalTargetAndMember(object? root,
        MemberExpression memberExpr)
    {
        // Stack to keep track of the member chain
        var members = new Stack<MemberExpression>();
        while (memberExpr != null)
        {
            members.Push(memberExpr);
            memberExpr = memberExpr.Expression as MemberExpression;
        }

        // Evaluate the intermediate objects
        var currentTarget = root;
        while (members.Count > 1) // Stop at the second-to-last member
        {
            var currentMember = members.Pop();
            var member = currentMember.Member;

            currentTarget = member switch
            {
                PropertyInfo property => property.GetValue(currentTarget),
                FieldInfo field => field.GetValue(currentTarget),
                _ => throw new InvalidOperationException("Unsupported member type.")
            };

            if (currentTarget == null) throw new NullReferenceException("Intermediate member is null.");
        }

        return (currentTarget, members.Pop().Member);
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