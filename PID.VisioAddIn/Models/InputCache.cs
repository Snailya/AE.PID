using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using Newtonsoft.Json;
using NLog;

namespace AE.PID.Models;

public class InputCache
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly string FilePath = Path.Combine(ThisAddIn.AppDataFolder, ".cache");

    public string CustomerName { get; set; }
    public string DocumentNo { get; set; }
    public string ProjectNo { get; set; }
    public string VersionNo { get; set; }

    public static InputCache Load()
    {
        var cache = new InputCache();
        if (!File.Exists(FilePath)) return cache;

        try
        {
            var configContent = File.ReadAllText(FilePath);

            if (!string.IsNullOrEmpty(configContent))
                cache = JsonConvert.DeserializeObject<InputCache>(configContent) ?? cache;
        }
        catch (JsonException jsonException)
        {
            Logger.Error(jsonException,
                $"Failed to log input cache.");
        }

        return cache;
    }


    public static void Save(InputCache cache)
    {
        using var configFileStream = File.Open(FilePath, FileMode.Create);
        using var configStreamWriter = new StreamWriter(configFileStream, Encoding.UTF8);
        var jsonString = JsonConvert.SerializeObject(cache, Formatting.Indented);
        configStreamWriter.Write(jsonString);
        configStreamWriter.Flush();
    }
}