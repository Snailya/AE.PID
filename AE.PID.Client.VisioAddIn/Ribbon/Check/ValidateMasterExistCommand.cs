using AE.PID.Client.Core;
using Microsoft.Office.Core;

namespace AE.PID.Client.VisioAddIn;

internal sealed class ValidateMasterExistCommand : RibbonCommandBase
{
    public override string Id { get; } = nameof(ValidateMasterExistCommand);

    public override void Execute(IRibbonControl control)
    {
        ErrorHelper.HighlightShapeLostMaster(Globals.ThisAddIn.Application.ActivePage);
    }

    public override bool CanExecute(IRibbonControl control)
    {
        return IsPageWindow() && !LayerExists(LayerDict.Validation);
    }


    public override string GetLabel(IRibbonControl control)
    {
        return "主控形状缺失";
    }
}