namespace AE.PID.Client.Core;

public interface IDataRow
{
    /// <summary>
    /// Convert the instance to a data array.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    object[] ToDataRow(int? index = null);
}