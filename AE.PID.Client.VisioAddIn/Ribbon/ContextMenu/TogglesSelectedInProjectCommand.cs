using AE.PID.Client.Core;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Client.VisioAddIn;

internal sealed class TogglesSelectedInProjectCommand : RibbonCommandBase
{
    public override string Id { get; } = nameof(TogglesSelectedInProjectCommand);

    public override void Execute(IRibbonControl control)
    {
        var target = Globals.ThisAddIn.Application.ActiveWindow.Selection[1];

        ClearColorFormulas(target);

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
    
    private static void ClearColorFormulas(IVShape shape)
    {
        // 2025.04.22: remove line settings.
        shape.Cells[CellDict.LineColor].FormulaU = "";
        shape.Cells[CellDict.TextColor].FormulaU = "";
    }
}