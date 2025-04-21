using AE.PID.Client.Core;
using Microsoft.Office.Core;

namespace AE.PID.Client.VisioAddIn;

public class ToggleIsOptionalCommand : RibbonCommandBase
{
    public override void Execute(IRibbonControl control)
    {
        var target = Globals.ThisAddIn.Application.ActiveWindow.Selection[1];

        var currentValue = target.TryGetValue<bool>(CellDict.IsOptional);

        if (currentValue is null or false)
            target.TrySetValue(CellDict.IsOptional, true, true);
        else
            target.TrySetValue(CellDict.IsOptional, false);
    }

    public override bool CanExecute(IRibbonControl control)
    {
        return IsPageWindow() && IsSingleSelection() && AreLocations();
    }

    public override string GetLabel(IRibbonControl control)
    {
        var target = Globals.ThisAddIn.Application.ActiveWindow.Selection[1];

        var currentValue = target.TryGetValue<bool>(CellDict.IsOptional);
        return currentValue is null or false ? "选配" : "标配";
    }
}