using System.Linq;
using AE.PID.Client.Core;
using AE.PID.Client.UI.Avalonia.VisioExt;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Client.VisioAddIn;

[RibbonContextMenu("MaterialGroup", "物料")]
internal class OpenMaterialDataPaneCommand : RibbonCommandBase
{
    public override string Id { get; } = nameof(OpenMaterialDataPaneCommand);

    public override void Execute(IRibbonControl control)
    {
        WindowHelper.ShowTaskPane<MaterialPaneView, MaterialPaneViewModel>("物料",
            (shape, vm) =>
            {
                if (shape != null)
                    vm.Code = shape.TryGetValue(CellDict.MaterialCode) ?? string.Empty;
            });
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
        return Globals.ThisAddIn.Application.ActiveWindow.Selection.OfType<IVShape>()
            .All(x => x.HasCategory("Equipment") || x.HasCategory("Instrument") || x.HasCategory("FunctionalElement"));
    }

    public override string GetLabel(IRibbonControl control)
    {
        return "详情";
    }
}