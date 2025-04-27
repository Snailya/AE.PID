using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AE.PID.Client.Core;
using AE.PID.Core;
using Microsoft.Office.Interop.Visio;
using Splat;
using Page = Microsoft.Office.Interop.Visio.Page;
using Shape = Microsoft.Office.Interop.Visio.Shape;


namespace AE.PID.Client.VisioAddIn;

public abstract class ErrorHelper
{
    private const string HighlightShapeWithDuplicatedDesignationScope =
        "Highlight Shape With Duplicated Designation Within Group";

    private const string RemoveUselessLocalValuesScope = "Remove Useless Local Values";
    private const string HighlightShapeLostMasterScope = "Highlight Shape Lost Master";
    private const string HighlightPipelineWithFormulaErrorScope = "Highlight Pipeline With Formula Error";

    /// <summary>
    ///     Clear the masks on the validation layer.
    /// </summary>
    /// <param name="page"></param>
    public static void ClearCheckMarks(IVPage page)
    {
        var selection = page.CreateSelection(VisSelectionTypes.visSelTypeByLayer, VisSelectMode.visSelModeSkipSuper,
            LayerDict.Validation);
        if (selection.Count > 0)
            selection.Delete();
    }

    /// <summary>
    ///     The designation for equipments should be unique within a functional group.
    ///     To help user locate the equipment with the wrong designation number, a mask will be placed on the duplicated
    ///     equipments.
    /// </summary>
    /// <param name="page"></param>
    public static void HighlightShapeWithDuplicatedDesignationWithinGroup(IVPage page)
    {
        var undoScope = page.Application.BeginUndoScope(HighlightShapeWithDuplicatedDesignationScope);

        try
        {
            var duplicated = page.Shapes.OfType<Shape>()
                .Where(x => (x.HasCategory("Equipment") || x.HasCategory("Equipments") || x.HasCategory("Instrument") ||
                             x.HasCategory("Instruments")) &&
                            !string.IsNullOrEmpty(x.CellsU[CellDict.FunctionElement]
                                .ResultStr[VisUnitCodes.visUnitsString]) &&
                            !string.IsNullOrEmpty(x.CellsU[CellDict.FunctionGroup]
                                .ResultStr[VisUnitCodes.visUnitsString]))
                .Select(x => new
                {
                    x.ID,
                    FunctionalElement = x.TryGetFormatValue(CellDict.FunctionElement),
                    FunctionalGroup = x.TryGetValue(CellDict.FunctionGroup)
                })
                .GroupBy(x => new { x.FunctionalGroup, x.FunctionalElement })
                .Where(x => x.Count() != 1)
                .ToList();

            if (duplicated.Count != 0)
            {
                var validationLayer = EnsureValidationLayerExist(page);

                foreach (var item in duplicated.SelectMany(x => x))
                    HighlightShapeById(page, item.ID, validationLayer);
            }
            else
            {
                MessageBox.Show("未发现异常。", "检查", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            page.Application.EndUndoScope(undoScope, true);
        }
        catch (Exception ex)
        {
            page.Application.EndUndoScope(undoScope, false);

            // log
            LogHost.Default.Error(ex, "Failed to check designation unique");

            // display error message
            MessageBox.Show(ex.Message, "检查失败：无法验证设备编号的唯一性。", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    ///     Remove the local values of the non-customizable cell value.
    ///     This is used to solve the historical bugs during the previous document update.
    /// </summary>
    /// <param name="page"></param>
    public static void RemoveUselessLocalValues(IVPage page)
    {
        var undoScope = Globals.ThisAddIn.Application.BeginUndoScope(RemoveUselessLocalValuesScope);

        try
        {
            page.Application.ShowChanges = false;

            var shapes = page.Shapes.OfType<Shape>().Where(x => x.Master != null).ToList();
            var srcList = new List<short>();

            foreach (var shape in shapes)
                // get the prop section
                if (shape.SectionExists
                        [(short)VisSectionIndices.visSectionProp, (short)VisExistsFlags.visExistsLocally] ==
                    (short)VBABool.True)
                {
                    var id = (short)shape.ID;
                    var section = shape.Section[(short)VisSectionIndices.visSectionProp];

                    for (short i = 0; i < section.Count; i++)
                        srcList.AddRange(new[]
                        {
                            id, (short)VisSectionIndices.visSectionProp, i, (short)VisCellIndices.visCustPropsFormat,
                            id, (short)VisSectionIndices.visSectionProp, i, (short)VisCellIndices.visCustPropsLabel,
                            id, (short)VisSectionIndices.visSectionProp, i, (short)VisCellIndices.visCustPropsPrompt,
                            id, (short)VisSectionIndices.visSectionProp, i, (short)VisCellIndices.visCustPropsSortKey,
                            id, (short)VisSectionIndices.visSectionProp, i, (short)VisCellIndices.visCustPropsInvis
                        });
                }

            Array? srcArray = srcList.ToArray();
            Array? formulaArr = Enumerable.Repeat<object>("", srcArray.Length / 4).ToArray();

            page.SetFormulas(ref srcArray, ref formulaArr, (short)VisGetSetArgs.visSetBlastGuards);

            page.Application.EndUndoScope(undoScope, true);
            page.Application.ShowChanges = true;

            MessageBox.Show("完成。", "检查", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            page.Application.EndUndoScope(undoScope, false);

            // log
            LogHost.Default.Error(ex, "Failed to remove duplicated local values.");

            // display error message
            MessageBox.Show(ex.Message, "失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    ///     Highlight the shapes that ought to have a master but not.
    ///     This problem raised because the previous document update bug. After highlighting these shapes, user should replace
    ///     them with new master manually.
    /// </summary>
    /// <param name="page"></param>
    public static void HighlightShapeLostMaster(Page page)
    {
        var undoScope = page.Application.BeginUndoScope("高亮主控形状缺失");

        try
        {
            var noMasters = page.Shapes.OfType<Shape>()
                .Where(x => x.Master == null && x.CellExistsN(CellDict.Class, VisExistsFlags.visExistsAnywhere))
                .ToList();

            var issueCount = noMasters.Count;

            if (issueCount > 0)
            {
                var validationLayer = EnsureValidationLayerExist(page);
                foreach (var item in noMasters)
                    HighlightShapeById(page, item.ID, validationLayer);

                var message = $"发现 {issueCount} 个形状缺失主控形状。\n\n" +
                              "这些形状已高亮显示。\n" +
                              "请使用\"开始\"-\"更改形状\"手动替换或使用\"加载项\"-\"修复\"-\"主控形状缺失\"功能尝试修复这个问题。";

                MessageBox.Show(message,
                    "检查结果 - 发现问题",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show("检查完成，未发现缺失主控形状的形状。",
                    "检查结果 - 正常",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            page.Application.EndUndoScope(undoScope, true);
        }
        catch (Exception ex)
        {
            page.Application.EndUndoScope(undoScope, false);

            LogHost.Default.Error(ex, "扫描缺失主控形状失败");

            var errorMessage = $"扫描过程中发生错误:\n\n{ex.Message}\n\n" +
                               "已撤销所有更改。\n" +
                               "如果问题持续存在，请联系技术支持并提供日志文件。";

            MessageBox.Show(errorMessage,
                "检查失败",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    ///     Scan for the pipeline with formula error.
    /// </summary>
    /// <param name="page"></param>
    public static void HighlightPipelineWithFormulaError(Page page)
    {
        var undoScope = Globals.ThisAddIn.Application.BeginUndoScope(HighlightPipelineWithFormulaErrorScope);
        var errorPipelinesFound = 0;
        var errorDetails = new List<string>();

        try
        {
            // 获取所有管路和信号线形状
            var pipelines = page.Shapes.OfType<Shape>()
                .Where(x => x.Master != null &&
                            (x.Master.BaseID == BaseIdDict.Pipe || x.Master.BaseID == BaseIdDict.Signal))
                .ToList();

            var totalPipelinesChecked = pipelines.Count;
            var validationLayer = EnsureValidationLayerExist(page);

            foreach (var pipeline in pipelines.Where(p => p.OneD == (short)VBABool.True))
            {
                // 检查起点坐标公式是否为"0 mm"
                var formulaBeginX = pipeline.CellsSRCN(VisSectionIndices.visSectionObject,
                    VisRowIndices.visRowXForm1D, VisCellIndices.vis1DBeginX).FormulaU;
                var formulaBeginY = pipeline.CellsSRCN(VisSectionIndices.visSectionObject,
                    VisRowIndices.visRowXForm1D, VisCellIndices.vis1DBeginY).FormulaU;

                if (formulaBeginX == "0 mm" && formulaBeginY == "0 mm")
                {
                    errorPipelinesFound++;
                    errorDetails.Add($"ID {pipeline.ID}: {pipeline.Name ?? "未命名管路"}");
                    HighlightShapeById(page, pipeline.ID, validationLayer);
                }
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
                var errorList = string.Join("\n", errorDetails.Take(10)); // 最多显示10条错误
                if (errorDetails.Count > 10) errorList += $"\n...以及另外 {errorDetails.Count - 10} 条";

                message = $"发现 {errorPipelinesFound}/{totalPipelinesChecked} 条管路/信号线存在公式错误:\n\n" +
                          $"{errorList}\n\n" +
                          "这些有问题的形状已在验证层中高亮显示。\n" +
                          "请检查这些形状的起点坐标公式设置。";
            }

            MessageBox.Show(message, "检查结果 - 发现问题", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            page.Application.EndUndoScope(undoScope, true);
        }
        catch (Exception ex)
        {
            page.Application.EndUndoScope(undoScope, false);

            // log
            LogHost.Default.Error(ex, "Failed to scan pipeline.");

            // display error message
            MessageBox.Show(ex.Message, "检查失败：无法完成管路公式错误检查。", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static Layer EnsureValidationLayerExist(IVPage page)
    {
        // create validation layer if not exist
        var layer = page.Layers.OfType<Layer>().SingleOrDefault(x => x.Name == LayerDict.Validation) ??
                    page.Layers.Add(LayerDict.Validation);
        layer.CellsC[2].FormulaU = "2"; // set layer color
        layer.CellsC[11].FormulaU = "50%"; // set layer transparency
        ClearCheckMarks(page);
        return layer;
    }

    private static void HighlightShapeById(IVPage page, int id, Layer validationLayer)
    {
        var (left, bottom, right, top) = page.Shapes.ItemFromID[id]
            .BoundingBoxMetric(
                (short)VisBoundingBoxArgs.visBBoxDrawingCoords + (short)VisBoundingBoxArgs.visBBoxExtents);
        var rect = page.DrawRectangleMetric(left - 1, bottom - 1, right + 1, top + 1);
        // set as transparent fill
        rect.CellsSRCN(VisSectionIndices.visSectionObject, VisRowIndices.visRowFill, VisCellIndices.visFillPattern)
            .FormulaU = "9";

        // set layer
        rect.CellsSRCN(VisSectionIndices.visSectionObject, VisRowIndices.visRowLayerMem, VisCellIndices.visLayerMember)
                .FormulaU = $"\"{validationLayer.Index - 1}\"";
    }
}