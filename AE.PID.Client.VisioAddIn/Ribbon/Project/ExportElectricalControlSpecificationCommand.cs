using Microsoft.Office.Core;

namespace AE.PID.Client.VisioAddIn;

internal sealed class ExportElectricalControlSpecificationCommand : RibbonCommandBase
{
    public override string Id { get; } = nameof(ExportElectricalControlSpecificationCommand);

    public override void Execute(IRibbonControl control)
    {
        ElectricalControlSpecificationHelper.Generate(Globals.ThisAddIn.Application.ActiveDocument);
    }

    public override bool CanExecute(IRibbonControl control)
    {
        return IsPageWindow();
    }

    public override string GetLabel(IRibbonControl control)
    {
        return "电控任务书";
    }
}