using System;
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
public class Configuration
{
    [JsonIgnore] public NLogConfiguration NLogConfig;

    /// <summary>
    /// The next time checking for app update.
    /// </summary>
    public DateTime NextCheck { get; set; }

#if DEBUG
    [JsonIgnore] public string Api { get; set; } = "http://localhost:32768";
#else
    /// <summary>
    ///     The base url for server.
    /// </summary>
    [JsonIgnore]
    public string Api { get; set; } = "http://172.18.128.104:32768";
#endif
    /// <summary>
    ///     The version of the app which used to check for update of the app.
    /// </summary>
    [JsonIgnore]
    public Version Version { get; set; } = new(0, 1, 0, 0);

    /// <summary>
    ///     The configuration for library version check.
    /// </summary>
    public LibraryConfiguration LibraryConfiguration { get; set; } = new();

    /// <summary>
    ///     The settings for export.
    /// </summary>
    public ExportSettings ExportSettings { get; set; } = new();


    [JsonIgnore] private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly string ConfigFileName = "ae-pid.json";

#if DEBUG
    private static readonly NLogConfiguration.LogLevel VerboseLogLevel = NLogConfiguration.LogLevel.Trace;
#else
        private static readonly NLogConfiguration.LogLevel verboseLogLevel = NLogConfiguration.LogLevel.Info;
#endif

    /// <summary>
    ///     Loads the configuration from file.
    /// </summary>
    /// <returns>An Configuration object.</returns>
    public static Configuration Load()
    {
        var config = new Configuration();

        if (File.Exists(GetConfigurationPath()))
            try
            {
                var configContent = File.ReadAllText(GetConfigurationPath());

                if (!string.IsNullOrEmpty(configContent))
                    config = JsonSerializer.Deserialize<Configuration>(configContent, new JsonSerializerOptions
                    {
                        Converters =
                        {
                            new ConcurrentBagConverter()
                        }
                    });
            }
            catch (Exception e)
            {
                if (e is not FileNotFoundException)
                    Logger.LogUsefulException(e);
            }

        // load nlogconfig, no need to call a process config
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
            using var configFileStream = File.Open(GetConfigurationPath(), FileMode.Create);
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

            Logger.Info($"Configuration saved successfully at path: '{GetConfigurationPath()}'");
        }
        catch (Exception ex)
        {
            Logger.Error("Unable to save configuration.");
            Logger.LogUsefulException(ex);
        }
    }

    /// <summary>
    ///     Get the absolute path of the configuration file.
    /// </summary>
    /// <returns></returns>
    public static string GetConfigurationPath()
    {
        return Path.Combine(Globals.ThisAddIn.DataFolder, ConfigFileName);
    }
}