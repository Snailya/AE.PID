using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Xml;
using AE.PID.Properties;
using NLog;
using NLog.Common;

namespace AE.PID.Models.Configurations;

public class NLogConfiguration
{
    public enum LogLevel
    {
        Fatal,
        Error,
        Warn,
        Info,
        Debug,
        Trace
    }

    private const string TargetMinLevelAttribute = "minlevel";

    private static readonly string NlogConfigFilepath =
        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "NLog.config");

    private readonly XmlDocument _doc = new();

    private XmlElement _logLevelElement;

    public static void SaveXml(NLogConfiguration nLogConfig)
    {
        nLogConfig._doc.Save(NlogConfigFilepath);
    }

    /// <summary>
    ///     Load the NLog config xml file content
    /// </summary>
    public static NLogConfiguration LoadXml()
    {
        NLogConfiguration config = new();
        config._doc.Load(NlogConfigFilepath);
        config._logLevelElement = (XmlElement)SelectSingleNode(config._doc, "//nlog:logger[@name='*']");

        return config;
    }

    /// <summary>
    ///     Select a single XML node/elemant
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="xpath"></param>
    /// <returns></returns>
    private static XmlNode SelectSingleNode(XmlDocument doc, string xpath)
    {
        XmlNamespaceManager manager = new(doc.NameTable);
        manager.AddNamespace("nlog", "http://www.nlog-project.org/schemas/NLog.xsd");
        return doc.SelectSingleNode(xpath, manager);
    }

    /// <summary>
    ///     Get the current minLogLevel from xml file
    /// </summary>
    /// <returns></returns>
    public LogLevel GetLogLevel()
    {
        var levelStr = _logLevelElement.GetAttribute(TargetMinLevelAttribute);
        Enum.TryParse(levelStr, out LogLevel level);
        return level;
    }

    /// <summary>
    ///     Extract the pre-defined NLog configuration file is does not exist. Then reload the Nlog configuration.
    /// </summary>
    public static void CreateIfNotExist()
    {
        try
        {
            if (File.Exists(NlogConfigFilepath))
                return; // NLog.config exists, and has already been loaded

            Directory.CreateDirectory(Path.GetDirectoryName(NlogConfigFilepath)!);
            File.WriteAllText(NlogConfigFilepath, Resources.NLog_config);
        }
        catch (PathTooLongException)
        {
            throw;
        }
        catch (SecurityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            InternalLogger.Error(ex, "[PID Visio AddIn] Failed to setup default NLog.config: {0}",
                NlogConfigFilepath);
        }
    }

    /// <summary>
    ///     NLog reload the config file and apply to current LogManager
    /// </summary>
    public static void Load()
    {
        LogManager.Setup().LoadConfigurationFromFile(NlogConfigFilepath);
    }
}