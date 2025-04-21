using Microsoft.Office.Core;

namespace AE.PID.Client.VisioAddIn;

public interface IRibbonCommand
{
    public void Execute(IRibbonControl control);

    public bool CanExecute(IRibbonControl control);

    public string GetLabel(IRibbonControl control);
}
