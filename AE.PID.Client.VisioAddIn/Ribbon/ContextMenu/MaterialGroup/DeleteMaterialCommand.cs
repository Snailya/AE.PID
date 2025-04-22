using System;
using System.Linq;
using AE.PID.Client.Core;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Client.VisioAddIn;

[RibbonContextMenu("MaterialGroup", "物料")]
internal class DeleteMaterialCommand : RibbonCommandBase
{
    public override string Id { get; } = nameof(DeleteMaterialCommand);

    public override void Execute(IRibbonControl control)
    {
        foreach (var shape in Globals.ThisAddIn.Application.ActiveWindow.Selection.OfType<IVShape>())
        {
            if (!shape.CellExistsN(CellDict.MaterialCode, VisExistsFlags.visExistsLocally)) continue;
            shape.TrySetValue(CellDict.MaterialCode, "");
        }
    }

    public override bool CanExecute(IRibbonControl control)
    {
        var selected = Globals.ThisAddIn.Application.ActiveWindow.Selection.OfType<IVShape>();
        return selected.Any(x =>
            x.CellExistsN(CellDict.MaterialCode, VisExistsFlags.visExistsLocally) &&
            !string.IsNullOrEmpty(x.Cells[CellDict.MaterialCode].ResultStr[VisUnitCodes.visUnitsString]));
    }

    public override bool GetVisible(IRibbonControl control)
    {
        return CanExecute(control);
    }

    public override string GetLabel(IRibbonControl control)
    {
        return "删除";
    }
}