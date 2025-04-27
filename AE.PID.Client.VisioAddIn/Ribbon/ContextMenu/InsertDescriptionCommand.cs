using AE.PID.Core;
using Microsoft.Office.Core;

namespace AE.PID.Client.VisioAddIn;

internal sealed class InsertDescriptionCommand : RibbonCommandBase
{
    public override string Id { get; } = nameof(InsertDescriptionCommand);

    public override void Execute(IRibbonControl control)
    {
        var target = Globals.ThisAddIn.Application.ActiveWindow.Selection[1];
        FormatHelper.InsertPCITables(target.ContainingPage, target!);
    }

    public override bool CanExecute(IRibbonControl control)
    {
        if (!IsPageWindow() || !IsSingleSelection()) return false;

        var target = Globals.ThisAddIn.Application.ActiveWindow.Selection[1];
        return target is { Master.BaseID: BaseIdDict.Frame };
    }

    public override string GetLabel(IRibbonControl control)
    {
        return "插入PCI说明";
    }

    public override bool GetVisible(IRibbonControl control)
    {
        return CanExecute(control);
    }
}