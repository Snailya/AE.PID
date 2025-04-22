using System.Linq;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Visio;
using Shape = Microsoft.Office.Interop.Visio.Shape;

namespace AE.PID.Client.VisioAddIn;

internal abstract class RibbonCommandBase : IRibbonCommand
{
    public abstract string Id { get; }

    public abstract void Execute(IRibbonControl control);

    public virtual bool CanExecute(IRibbonControl control)
    {
        return true;
    }

    public abstract string GetLabel(IRibbonControl control);

    public virtual bool GetVisible(IRibbonControl control)
    {
        return true;
    }

    protected static bool IsSingleSelection()
    {
        return Globals.ThisAddIn.Application.ActiveWindow.Selection.Count == 1;
    }

    protected static bool IsPageWindow()
    {
        return Globals.ThisAddIn.Application.ActiveWindow?.SubType == (short)VisWinTypes.visPageWin;
    }

    protected static bool AreLocations()
    {
        return Globals.ThisAddIn.Application.ActiveWindow.Selection.OfType<Shape>().All(x => x.IsValidLocation());
    }

    protected static bool LayerExists(string layerName)
    {
        var selection = Globals.ThisAddIn.Application.ActivePage.CreateSelection(VisSelectionTypes.visSelTypeByLayer,
            VisSelectMode.visSelModeSkipSuper, layerName);
        return selection.Count > 0;
    }
}