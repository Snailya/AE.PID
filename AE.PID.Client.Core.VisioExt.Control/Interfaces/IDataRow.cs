namespace AE.PID.Client.Core.VisioExt.Control;

public interface IDataRow
{
    object[] ToDataRow(int? index = null);
}