using System.IO;
using System.Windows.Forms;
using Microsoft.Office.Core;

namespace AE.PID.Client.VisioAddIn.Fix;

internal sealed class FixEndOfFileCommand : RibbonCommandBase
{
    public override string Id { get; } = nameof(FixEndOfFileCommand);

    public override void Execute(IRibbonControl control)
    {
        // 2025.3.17：出现意外的文件尾错误时，将文件另存为兼容格式再另存为原格式。
        var fileName = Globals.ThisAddIn.Application.ActiveDocument.FullName;
        var compatibilityFileName = Path.ChangeExtension(fileName, "vsd");
        Globals.ThisAddIn.Application.ActiveDocument.SaveAs(compatibilityFileName);
        Globals.ThisAddIn.Application.ActiveDocument.SaveAs(fileName);

        // delete the compatibility file
        File.Delete(compatibilityFileName);

        MessageBox.Show("完成", "修复");
    }

    public override bool CanExecute(IRibbonControl control)
    {
        return IsPageWindow();
    }

    public override string GetLabel(IRibbonControl control)
    {
        return "意外的文件尾";
    }
}