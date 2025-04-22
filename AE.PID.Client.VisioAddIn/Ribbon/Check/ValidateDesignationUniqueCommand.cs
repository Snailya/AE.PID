using AE.PID.Client.Core;
using Microsoft.Office.Core;

namespace AE.PID.Client.VisioAddIn;

internal sealed class ValidateDesignationUniqueCommand : RibbonCommandBase
{
    public override string Id { get; } = nameof(ValidateDesignationUniqueCommand);

    public override void Execute(IRibbonControl control)
    {
        ErrorHelper.HighlightShapeWithDuplicatedDesignationWithinGroup(Globals.ThisAddIn.Application.ActivePage);
    }

    public override bool CanExecute(IRibbonControl control)
    {
        return IsPageWindow() && !LayerExists(LayerDict.Validation);
    }


    public override string GetLabel(IRibbonControl control)
    {
        return "位号重复";
    }
}