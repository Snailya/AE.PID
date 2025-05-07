using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using AE.PID.Core;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Visio;
using Splat;

namespace AE.PID.Client.VisioAddIn.Fix;

internal sealed class FixPipeCommand : RibbonCommandBase, IEnableLogger
{
    private readonly Regex _regex = new(@"([-+]?\d+\.?\d*|[-+]?\.\d+) mm");

    public override string Id { get; } = nameof(FixPipeCommand);

    public override void Execute(IRibbonControl control)
    {
        Globals.ThisAddIn.Application.ShowChanges = false;

        var scope = Globals.ThisAddIn.Application.BeginUndoScope("修复管路");

        var totalPipelinesChecked = 0;
        var errorPipelinesFound = 0;

        try
        {
            foreach (var page in Globals.ThisAddIn.Application.ActiveDocument.Pages.OfType<Page>())
            foreach (var shape in page.Shapes.OfType<IVShape>()
                         .Where(x => x.Master != null && x.Master.BaseID is BaseIdDict.Pipe or BaseIdDict.Signal))
            {
                var isAbnormal = false;
                var shapeId = shape.ID;

                var beginXCell = shape.CellsN(VisSectionIndices.visSectionObject,
                    VisRowIndices.visRowXForm1D, VisCellIndices.vis1DBeginX);
                var beginYCell = shape.CellsN(VisSectionIndices.visSectionObject,
                    VisRowIndices.visRowXForm1D, VisCellIndices.vis1DBeginY);
                
                if (beginXCell.IsInherited == (short)VBABool.True)
                {
                    isAbnormal = true;
                    beginXCell.FormulaU = beginXCell.ResultStr[beginXCell.Units];
                }

                if (beginYCell.IsInherited == (short)VBABool.True)
                {
                    isAbnormal = true;
                    beginYCell.FormulaU = beginYCell.ResultStr[beginYCell.Units];
                }
                
                if (isAbnormal)
                    errorPipelinesFound += 1;

                totalPipelinesChecked += 1;
            }

            // 构建用户反馈信息
            string message;
            if (totalPipelinesChecked == 0)
            {
                message = "当前页面未找到任何管路或信号线。";
            }
            else if (errorPipelinesFound == 0)
            {
                message = $"检查完成，已扫描 {totalPipelinesChecked} 条管路/信号线，未发现公式错误。";
                MessageBox.Show(message, "检查结果 - 正常", MessageBoxButtons.OK);
            }
            else
            {
                message = $"发现 {errorPipelinesFound}/{totalPipelinesChecked} 条管路/信号线存在公式错误:\n\n" +
                          "这些有问题的形状已修复。";
            }

            MessageBox.Show(message, "检查结果 - 发现问题", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            Globals.ThisAddIn.Application.EndUndoScope(scope, true);
        }
        catch (Exception ex)
        {
            Globals.ThisAddIn.Application.EndUndoScope(scope, false);

            // log
            LogHost.Default.Error(ex, "Failed to scan pipeline.");

            // display error message
            MessageBox.Show(ex.Message, "检查失败：无法完成管路公式错误检查。", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Globals.ThisAddIn.Application.ShowChanges = true;
        }
    }


    private static Array CreateArray<T>(int length, T initialValue)
    {
        // ReSharper disable once UseArrayCreationExpression.1
        var targetArray = Array.CreateInstance(typeof(object), length);
        Array.Copy(
            Enumerable.Repeat(initialValue, length).ToArray(), // 源数组
            targetArray, // 目标数组
            targetArray.Length // 复制元素数量
        );
        return targetArray;
    }


    public override bool CanExecute(IRibbonControl control)
    {
        return IsPageWindow();
    }

    public override string GetLabel(IRibbonControl control)
    {
        return "修复管路";
    }
}