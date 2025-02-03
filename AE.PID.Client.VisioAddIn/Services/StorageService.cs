using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using AE.PID.Client.VisioAddIn.Properties;
using MiniExcelLibs;
using Splat;

namespace AE.PID.Client.VisioAddIn;

public class StorageService : IStorageService, IEnableLogger
{
    /// <inheritdoc />
    public async Task SaveAsWorkbookAsync(string fileName, object data)
    {
        await MiniExcel.SaveAsByTemplateAsync(fileName, Resources.TEMPLATE_Parts_List, data);
    }

    /// <inheritdoc />
    public void SaveAsJson<T>(string fileName, T data)
    {
        // serialize the data into json string
        var str = JsonSerializer.Serialize(data);

        // create the folder if the file is not an existed file and the directory is also not exist
        if (!File.Exists(fileName) && Path.GetDirectoryName(fileName) is { } directoryName &&
            !Directory.Exists(directoryName)) Directory.CreateDirectory(directoryName);

        using var configFileStream = File.Open(fileName, FileMode.Create);
        using var configStreamWriter = new StreamWriter(configFileStream, Encoding.UTF8);
        configStreamWriter.Write(str);
        configStreamWriter.Flush();
    }
}