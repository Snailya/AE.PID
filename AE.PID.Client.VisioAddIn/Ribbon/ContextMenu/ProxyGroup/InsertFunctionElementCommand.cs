using System.Linq;
using AE.PID.Client.Core.VisioExt;
using AE.PID.Core;
using Microsoft.Office.Core;

namespace AE.PID.Client.VisioAddIn;
[RibbonContextMenu("ProxyGroup", "插入代理")]

internal sealed class InsertFunctionElementCommand : RibbonCommandBase
{
    public override string Id { get; } = nameof(InsertFunctionElementCommand);

    public override void Execute(IRibbonControl control)
    {
        var target = Globals.ThisAddIn.Application.ActiveWindow.Selection[1];

        ProxyHelper.Insert(target, FunctionType.FunctionElement);
    }

    public override bool CanExecute(IRibbonControl control)
    {
        if (!IsPageWindow() || !IsSingleSelection()) return false;

        var categories = Globals.ThisAddIn.Application.ActiveWindow.Selection[1]
            .GetCategories();
        return (!categories.Contains(VisioShapeCategory.Proxy) && categories.Contains(VisioShapeCategory.Equipment)) ||
               categories.Contains(VisioShapeCategory.Instrument);
    }

    public override string GetLabel(IRibbonControl control)
    {
        return "代理功能元件";
    }


    public override bool GetVisible(IRibbonControl control)
    {
        return CanExecute(control);
    }
}