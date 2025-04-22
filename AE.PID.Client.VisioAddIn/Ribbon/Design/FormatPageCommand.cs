using Microsoft.Office.Core;

namespace AE.PID.Client.VisioAddIn;

internal sealed class FormatPageCommand : RibbonCommandBase
{
    public override string Id { get; } = nameof(FormatPageCommand);

    public override void Execute(IRibbonControl control)
    {
        FormatHelper.FormatPage(Globals.ThisAddIn.Application.ActivePage);
    }

    public override bool CanExecute(IRibbonControl control)
    {
        return IsPageWindow();
    }

    public override string GetLabel(IRibbonControl control)
    {
        return "初始化";
    }
}