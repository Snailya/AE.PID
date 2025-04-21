using System.Linq;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Visio;
using Shape = Microsoft.Office.Interop.Visio.Shape;

namespace AE.PID.Client.VisioAddIn;

public abstract class RibbonCommandBase : IRibbonCommand
{
    public abstract void Execute(IRibbonControl control);

    public abstract bool CanExecute(IRibbonControl control);

    public abstract string GetLabel(IRibbonControl control);

    protected static bool IsSingleSelection()
    {
        return Globals.ThisAddIn.Application.ActiveWindow.Selection.Count == 1;
    }

    protected static bool IsPageWindow()
    {
        return Globals.ThisAddIn.Application.ActiveWindow.SubType == (short)VisWinTypes.visPageWin;
    }

    protected static bool AreLocations()
    {
        return Globals.ThisAddIn.Application.ActiveWindow.Selection.OfType<Shape>().All(x => x.IsValidLocation());
    }
}