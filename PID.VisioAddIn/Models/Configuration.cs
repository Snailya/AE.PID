using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AE.PID.Controllers;
using AE.PID.Converters;
using NLog;

namespace AE.PID.Models;

[Serializable]
public class Configuration : UpdatableConfigurationBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly string ConfigFileName = "ae-pid.json";
    private static readonly NLogConfiguration.LogLevel VerboseLogLevel = NLogConfiguration.LogLevel.Trace;

    private readonly object _configurationLock = new();

    [JsonIgnore] public NLogConfiguration NLogConfig;
    [JsonIgnore] public string Api { get; set; } = "http://172.18.128.104:32768";
    [JsonIgnore] public Version Version { get; set; } = new(0, 1, 0, 2);

    /// <summary>
    ///     The configuration for library version check.
    /// </summary>
    public LibraryConfiguration LibraryConfiguration { get; set; } = new();

    /// <summary>
    ///     The settings for export.
    /// </summary>
    public ExportSettings ExportSettings { get; set; } = new();

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
                    config = JsonSerializer.Deserialize<Configuration>(
                        configContent,
                        new JsonSerializerOptions
                        {
                            Converters = { new ConcurrentBagConverter() }
                        });
            }
            catch (JsonException jsonException)
            {
                Logger.Error(jsonException,
                    $"Failed to log config from {GetPath()}, a default configuration file is used instead.");
            }

        config.NLogConfig = NLogConfiguration.LoadXml();

        LogManager.GetCurrentClassLogger().Info("Configuration loaded.");

        return config;
    }

    /// <summary>
    ///     Saves the Configuration object to file.
    /// </summary>
    /// <param name="config">A reference of Configuration object.</param>
    public static void Save(Configuration config)
    {
        try
        {
            // app config
            using var configFileStream = File.Open(GetPath(), FileMode.Create);
            using var configStreamWriter = new StreamWriter(configFileStream, Encoding.UTF8);
            var jsonString = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 中文字不編碼
                WriteIndented = true // 換行與縮排
            });
            configStreamWriter.Write(jsonString);
            configStreamWriter.Flush();

            // nlog config
            NLogConfiguration.SaveXml(config.NLogConfig);

            Logger.Info($"Configuration saved successfully at path: '{GetPath()}'");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to save configuration");
        }
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