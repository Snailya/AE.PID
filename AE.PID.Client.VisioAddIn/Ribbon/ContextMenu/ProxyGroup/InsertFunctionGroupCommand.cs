using System.Linq;
using AE.PID.Client.Core.VisioExt;
using AE.PID.Core;
using Microsoft.Office.Core;

namespace AE.PID.Client.VisioAddIn;

[RibbonContextMenu("ProxyGroup", "插入代理")]
internal sealed class InsertFunctionGroupCommand : RibbonCommandBase
{
    public override string Id { get; } = nameof(InsertFunctionGroupCommand);

    public override void Execute(IRibbonControl control)
    {
        var target = Globals.ThisAddIn.Application.ActiveWindow.Selection[1];

        ProxyHelper.Insert(target, FunctionType.FunctionGroup);
    }

    public override bool CanExecute(IRibbonControl control)
    {
        return IsPageWindow() && IsSingleSelection() &&
               Globals.ThisAddIn.Application.ActiveWindow.Selection[1].GetCategories()
                   .Contains(VisioShapeCategory.FunctionalGroup);
    }

    public override string GetLabel(IRibbonControl control)
    {
        return "代理功能组";
    }


    public override bool GetVisible(IRibbonControl control)
    {
        return CanExecute(control);
    }
}