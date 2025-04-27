using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Visio;
using Splat;

namespace AE.PID.Client.VisioAddIn.Fix;

internal sealed class FixTextCommand : RibbonCommandBase, IEnableLogger
{
    private readonly Regex _regex = new(@"([-+]?\d+\.?\d*|[-+]?\.\d+) mm");

    public override string Id { get; } = nameof(FixTextCommand);

    public override void Execute(IRibbonControl control)
    {
        Globals.ThisAddIn.Application.ShowChanges = false;

        var scope = Globals.ThisAddIn.Application.BeginUndoScope("刷新文字");

        try
        {
            foreach (var shape in Globals.ThisAddIn.Application.ActiveDocument.Pages.OfType<IVPage>()
                         .SelectMany(x =>
                             x.Shapes.OfType<IVShape>()
                                 .Where(i => i.Master != null)
                                 .SelectMany(i => i.Shapes.OfType<IVShape>())
                                 .Where(i => i.Cells["Width"].FormulaU.Contains("TheText"))))
                shape.Cells["Width"].Trigger();

            MessageBox.Show("完成", "修复成功", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            Globals.ThisAddIn.Application.EndUndoScope(scope, true);
        }
        catch (Exception ex)
        {
            Globals.ThisAddIn.Application.EndUndoScope(scope, false);

            // log
            LogHost.Default.Error(ex, "Failed to force update text.");

            // display error message
            MessageBox.Show(ex.Message, "修复失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Globals.ThisAddIn.Application.ShowChanges = true;
        }
    }

    public override bool CanExecute(IRibbonControl control)
    {
        return IsPageWindow();
    }

    public override string GetLabel(IRibbonControl control)
    {
        return "修复文字";
    }
}