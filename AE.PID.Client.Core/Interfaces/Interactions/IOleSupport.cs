namespace AE.PID.Client.Core;

public interface IOleSupport
{
    /// <summary>
    ///     Insert the data as embedded Excel sheet at the active page.
    /// </summary>
    /// <param name="dataArray"></param>
    void InsertAsExcelSheet(string[,] dataArray);
}