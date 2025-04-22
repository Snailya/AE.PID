using Microsoft.Office.Core;

namespace AE.PID.Client.VisioAddIn;

internal sealed class InsertLegendCommand : RibbonCommandBase
{
    public override string Id { get; } = nameof(InsertLegendCommand);

    public override void Execute(IRibbonControl control)
    {
        LegendHelper.Insert(Globals.ThisAddIn.Application.ActivePage);
    }

    public override bool CanExecute(IRibbonControl control)
    {
        return IsPageWindow();
    }

    public override string GetLabel(IRibbonControl control)
    {
        return "图例";
    }
}