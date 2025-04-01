using System.Threading.Tasks;

namespace AE.PID.Client.Core;

public interface IExportService
{
    /// <summary>
    ///     Save the text at the specified file path.
    /// </summary>
    /// <param name="fileName">The full name of the file.</param>
    /// <param name="data"></param>
    void SaveAsJson<T>(string fileName, T data);

    void ExportAsPartLists(PartListItem[] parts, string filePath);
}