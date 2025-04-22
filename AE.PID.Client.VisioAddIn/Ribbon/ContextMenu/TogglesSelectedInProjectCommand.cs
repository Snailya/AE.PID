using AE.PID.Client.Core;
using Microsoft.Office.Core;

namespace AE.PID.Client.VisioAddIn;

internal sealed class TogglesSelectedInProjectCommand : RibbonCommandBase
{
    public override string Id { get; } = nameof(TogglesSelectedInProjectCommand);

    public override void Execute(IRibbonControl control)
    {
        var target = Globals.ThisAddIn.Application.ActiveWindow.Selection[1];

        var currentValue = target.TryGetValue<bool>(CellDict.IsSelectedInProject);
        if (currentValue is null or true)
            target.TrySetValue(CellDict.IsSelectedInProject, false, true);
        else
            target.TrySetValue(CellDict.IsSelectedInProject, true);
    }

    public override bool CanExecute(IRibbonControl control)
    {
        return IsPageWindow() && IsSingleSelection() && AreLocations();
    }

    public override string GetLabel(IRibbonControl control)
    {
        var target = Globals.ThisAddIn.Application.ActiveWindow.Selection[1];

        var currentValue = target.TryGetValue<bool>(CellDict.IsSelectedInProject);
        return currentValue is null or true ? "不配置" : "配置";
    }


    public override bool GetVisible(IRibbonControl control)
    {
        return CanExecute(control);
    }
}