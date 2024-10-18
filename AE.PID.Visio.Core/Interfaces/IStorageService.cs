namespace AE.PID.Visio.Core.Interfaces;

public interface IStorageService
{
    /// <summary>
    ///     Save the data as Excel workbook.
    /// </summary>
    /// <param name="fileName">The full name of the file.</param>
    /// <param name="data"></param>
    /// <returns></returns>
    Task SaveAsWorkbookAsync(string fileName, object data);

    /// <summary>
    ///     Save the text at the specified file path.
    /// </summary>
    /// <param name="fileName">The full name of the file.</param>
    /// <param name="data"></param>
    void SaveAsJson<T>(string fileName, T data);
}