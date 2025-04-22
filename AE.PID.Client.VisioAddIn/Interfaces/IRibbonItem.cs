using Microsoft.Office.Core;

namespace AE.PID.Client.VisioAddIn;

internal interface IRibbonItem
{
    string Id { get; }

    bool GetVisible(IRibbonControl control);

    string GetLabel(IRibbonControl control);
}