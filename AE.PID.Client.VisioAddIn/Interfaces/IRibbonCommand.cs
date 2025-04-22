using Microsoft.Office.Core;

namespace AE.PID.Client.VisioAddIn;

internal interface IRibbonCommand : IRibbonItem
{
    public void Execute(IRibbonControl control);

    public bool CanExecute(IRibbonControl control);
}