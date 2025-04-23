using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AE.PID.Client.Core;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Visio;
using Splat;

namespace AE.PID.Client.VisioAddIn.Fix;

internal sealed class FixMasterNotFoundCommand : RibbonCommandBase, IEnableLogger
{
    public override string Id { get; } = nameof(FixMasterNotFoundCommand);

    public override void Execute(IRibbonControl control)
    {
        var scope = Globals.ThisAddIn.Application.BeginUndoScope("修复主控形状缺失");
        var totalShapesProcessed = 0;
        var shapesFixed = 0;
        var shapesSkipped = 0;
        var missingMasters = new List<string>();
        var duplicateMasters = new List<string>();

        try
        {
            // 收集文档中所有可用主控形状
            var masterDict = Globals.ThisAddIn.Application.ActiveDocument.Masters.OfType<IVMaster>().Select(x =>
                    new { x.UniqueID, x.Name, Class = x.Shapes[1].TryGetValue(CellDict.Class) })
                .Where(x => !string.IsNullOrEmpty(x.Class))
                .GroupBy(x => x.Class)
                .ToDictionary(t => t.Key);

            // 处理所有缺失主控形状的实例
            foreach (var grouping in Globals.ThisAddIn.Application.ActiveDocument.Pages.OfType<IVPage>()
                         .SelectMany(x => x.Shapes.OfType<IVShape>()).Where(x =>
                             x.Master == null &&
                             x.CellExistsN(CellDict.Class, VisExistsFlags.visExistsAnywhere))
                         .Select(x => new { Class = x.TryGetValue(CellDict.Class), Shape = x })
                         .GroupBy(x => x.Class))
            {
                totalShapesProcessed += grouping.Count();

                var key = grouping.Key;

                if (key == null || string.IsNullOrEmpty(key))
                {
                    this.Log().Info($"Skip {grouping.Count()} items because the class property is empty");
                }
                else
                {
                    if (masterDict.TryGetValue(key, out var masters))
                    {
                        if (masters.Count() == 1)
                        {
                            var masterObject =
                                Globals.ThisAddIn.Application.ActiveDocument.Masters[$"U{masters.First().UniqueID}"]!;
                            foreach (var shape in grouping.Select(shape => shape.Shape))
                            {
                                shape.ReplaceShape(masterObject);
                                shapesFixed++;
                            }
                        }
                        else
                        {
                            shapesSkipped += grouping.Count();

                            var masterNames = string.Join(" | ", masters.Select(x => x.Name));
                            if (!duplicateMasters.Contains(masterNames))
                                duplicateMasters.Add(masterNames);

                            this.Log().Info(
                                $"Skip {grouping.Count()} items with class property {key} because there are duplicate masters in the document stencil.");
                        }
                    }
                    else
                    {
                        shapesSkipped += grouping.Count();
                        if (!missingMasters.Contains(key)) missingMasters.Add(key);

                        this.Log().Info(
                            $"Skip {grouping.Count()} items with class property {key} because the master does not exist in the document stencil.");
                    }
                }
            }

            Globals.ThisAddIn.Application.EndUndoScope(scope, true);

            // 构建更详细的用户反馈
            string message;
            if (totalShapesProcessed == 0)
            {
                message = "未找到需要修复的形状。";
            }
            else if (missingMasters.Count == 0)
            {
                message = $"成功修复了 {shapesFixed} 个形状。";
            }
            else
            {
                message = $"部分完成：\n\n" +
                          $"已修复：{shapesFixed} 个形状，未修复：{shapesSkipped} 个形状。\n\n";

                if (missingMasters.Count != 0)
                {
                    var missingMastersList = string.Join("\n", missingMasters.Select(m => $"• {m}"));
                    message += $"缺少以下主控形状：\n" +
                               $"{missingMastersList}\n" +
                               $"请确保这些主控形状存在于文档模具中。\n\n";
                }

                if (duplicateMasters.Count != 0)
                {
                    var duplicateMastersList = string.Join("\n", duplicateMasters.Select(m => $"• {m}"));
                    message += $"以下主控形状重复：\n" +
                               $"{duplicateMastersList}\n" +
                               $"请从文档模具中移除重复的主控形状。如果该主控形状是您根据模具库中的主控形状修改得到的，请修改其{CellDict.Class}属性的值。\n\n";
                }
            }

            MessageBox.Show(message, "修复主控形状缺失", MessageBoxButtons.OK,
                missingMasters.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            this.Log().Error(ex);

            Globals.ThisAddIn.Application.EndUndoScope(scope, false);

            var errorMessage = $"修复过程中发生错误:\n\n{ex.Message}\n\n" +
                               "已撤销所有更改。请查看日志获取详细信息。";
            MessageBox.Show(errorMessage, "修复失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public override bool CanExecute(IRibbonControl control)
    {
        return IsPageWindow();
    }

    public override string GetLabel(IRibbonControl control)
    {
        return "主控形状缺失";
    }
}