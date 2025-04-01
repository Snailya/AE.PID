using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AE.PID.Client.Core;
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
                    FunctionalElement = x.CellsU[CellDict.FunctionElement].TryGetFormatValue(),
                    FunctionalGroup = x.CellsU[CellDict.FunctionGroup].ResultStr[VisUnitCodes.visUnitsString]
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
        var undoScope = page.Application.BeginUndoScope(HighlightShapeLostMasterScope);

        try
        {
            var noMasters = page.Shapes.OfType<Shape>()
                .Where(x => x.CellExistsN("User.msvShapeCategories", VisExistsFlags.visExistsAnywhere))
                .Where(x => x.Master == null)
                .ToList();

            if (noMasters.Any())
            {
                var validationLayer = EnsureValidationLayerExist(page);
                foreach (var item in noMasters)
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
            LogHost.Default.Error(ex, "Failed to scan master lost.");

            // display error message
            MessageBox.Show(ex.Message, "检查失败：无法完成扫描丢失主控形状的操作。", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    ///     Scan for the pipeline with formula error.
    /// </summary>
    /// <param name="page"></param>
    public static void HighlightPipelineWithFormulaError(Page page)
    {
        var undoScope = Globals.ThisAddIn.Application.BeginUndoScope(HighlightPipelineWithFormulaErrorScope);

        try
        {
            var pipelines = page.Shapes.OfType<Shape>()
                .Where(x => x.Master != null &&
                            (x.Master.BaseID == BaseIdDict.Pipe || x.Master.BaseID == BaseIdDict.Signal))
                .ToList();

            var validationLayer = EnsureValidationLayerExist(page);
            var hasError = false;

            foreach (var pipeline in pipelines.Where(pipeline => pipeline.OneD == (short)VBABool.True))
            {
                var id = pipeline.ID;
                var beginX = pipeline.CellsSRCN(VisSectionIndices.visSectionObject, VisRowIndices.visRowXForm1D,
                    VisCellIndices.vis1DBeginX).ResultIU;
                var beginY = pipeline.CellsSRCN(VisSectionIndices.visSectionObject, VisRowIndices.visRowXForm1D,
                    VisCellIndices.vis1DBeginY).ResultIU;

                // Use FormulaU to get the formula to avoid regional issues
                var formulaBeginX = pipeline.CellsSRCN(VisSectionIndices.visSectionObject, VisRowIndices.visRowXForm1D,
                    VisCellIndices.vis1DBeginX).FormulaU;
                var formulaBeginY = pipeline.CellsSRCN(VisSectionIndices.visSectionObject, VisRowIndices.visRowXForm1D,
                    VisCellIndices.vis1DBeginY).FormulaU;

                if (formulaBeginX == "0 mm" && formulaBeginY == "0 mm")
                {
                    hasError = true;
                    HighlightShapeById(page, pipeline.ID, validationLayer);
                }
            }

            // Inform the user if no errors are found
            if (!hasError)
                MessageBox.Show("未发现异常。", "检查", MessageBoxButtons.OK, MessageBoxIcon.Information);

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