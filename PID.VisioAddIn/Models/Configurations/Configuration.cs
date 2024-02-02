using System;
using System.IO;
using System.Reactive.Subjects;
using System.Text;
using Newtonsoft.Json;
using NLog;

namespace AE.PID.Models.Configurations;

[Serializable]
public class Configuration : ConfigurationBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private const string ConfigFileName = "ae-pid.json";

    [JsonIgnore] public NLogConfiguration NLogConfig;
    [JsonIgnore] public string Api { get; set; } = "http://172.18.128.104:32768";
    [JsonIgnore] public Version Version { get; set; } = new(0, 3, 1, 0);

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
            var jsonString = JsonConvert.SerializeObject(this, Formatting.Indented);
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
        Configuration config = null;

        try
        {
            if (File.Exists(GetPath()))
            {
                var configContent = File.ReadAllText(GetPath());

                if (!string.IsNullOrEmpty(configContent))
                {
                    var localConfig = JsonConvert.DeserializeObject<Configuration>(configContent);
                    config = localConfig;
                }
            }
        }
        catch (Exception exception)
        {
            Logger.Error(exception,
                $"Failed to log config from {GetPath()}, a default configuration file is used instead.");
        }
        finally
        {
            config ??= new Configuration();
            config.NLogConfig = NLogConfiguration.LoadXml();
        }

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