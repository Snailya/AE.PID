using System;
using System.IO;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AE.PID.Converters;
using NLog;

namespace AE.PID.Models.Configurations;

[Serializable]
public class Configuration : ConfigurationBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly string ConfigFileName = "ae-pid.json";
    private static readonly NLogConfiguration.LogLevel VerboseLogLevel = NLogConfiguration.LogLevel.Trace;

    private readonly object _configurationLock = new();

    [JsonIgnore] public NLogConfiguration NLogConfig;
    [JsonIgnore] public string Api { get; set; } = "http://172.18.128.104:32768";
    [JsonIgnore] public Version Version { get; set; } = new(0, 2, 2, 0);

    [JsonIgnore] public BehaviorSubject<Configuration> ConfigurationSubject = new(null);

    /// <summary>
    ///     The configuration for library version check.
    /// </summary>
    public LibraryConfiguration LibraryConfiguration { get; set; } = new();

    /// <summary>
    ///     The settings for export.
    /// </summary>
    public ExportSettings ExportSettings { get; set; } = new();

    /// <summary>
    ///     Saves the Configuration object to file.
    /// </summary>
    public void Save()
    {
        try
        {
            // app config
            using var configFileStream = File.Open(GetPath(), FileMode.Create);
            using var configStreamWriter = new StreamWriter(configFileStream, Encoding.UTF8);
            var jsonString = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 中文字不編碼
                WriteIndented = true // 換行與縮排
            });
            configStreamWriter.Write(jsonString);
            configStreamWriter.Flush();

            // nlog config
            NLogConfiguration.SaveXml(NLogConfig);

            Logger.Info($"Configuration saved successfully at path: '{GetPath()}'");

            ConfigurationSubject.OnNext(this);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to save configuration");
        }
    }

    /// <summary>
    ///     Loads the configuration from file or create a new configuration if not exist.
    /// </summary>
    /// <returns>An Configuration object.</returns>
    public static Configuration Load()
    {
        var config = new Configuration();

        if (File.Exists(GetPath()))
            try
            {
                var configContent = File.ReadAllText(GetPath());

                if (!string.IsNullOrEmpty(configContent))
                {
                    var localConfig = JsonSerializer.Deserialize<Configuration>(
                        configContent,
                        new JsonSerializerOptions
                        {
                            Converters = { new ConcurrentBagConverter() }
                        });

                    if (localConfig != null)
                    {
                        config = localConfig;
                        LogManager.GetCurrentClassLogger().Debug("Local configuration loaded.");
                    }
                    else
                    {
                        LogManager.GetCurrentClassLogger().Debug("Default configuration loaded.");
                    }
                }
            }
            catch (JsonException jsonException)
            {
                Logger.Error(jsonException,
                    $"Failed to log config from {GetPath()}, a default configuration file is used instead.");
            }

        config.NLogConfig = NLogConfiguration.LoadXml();

#if DEBUG
        config.Api = "http://localhost:32768";
#endif

        config.ConfigurationSubject.OnNext(config);

        return config;
    }

    /// <summary>
    ///     Get the absolute path of the configuration file.
    /// </summary>
    /// <returns></returns>
    public static string GetPath()
    {
        return Path.Combine(ThisAddIn.AppDataFolder, ConfigFileName);
    }
}