using AE.PID.Client.Core;
using Microsoft.Office.Core;

namespace AE.PID.Client.VisioAddIn;

internal sealed class ValidatePipelineCommand : RibbonCommandBase
{
    public override string Id { get; } = nameof(ValidatePipelineCommand);

    public override void Execute(IRibbonControl control)
    {
        ErrorHelper.HighlightPipelineWithFormulaError(Globals.ThisAddIn.Application.ActivePage);
    }

    public override bool CanExecute(IRibbonControl control)
    {
        return IsPageWindow() && !LayerExists(LayerDict.Validation);
    }


    public override string GetLabel(IRibbonControl control)
    {
        return "异常管线";
    }
}