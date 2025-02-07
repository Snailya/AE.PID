using System;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Office.Interop.Visio;
using Splat;
using Page = Microsoft.Office.Interop.Visio.Page;
using Shape = Microsoft.Office.Interop.Visio.Shape;

namespace AE.PID.Client.VisioAddIn;

public abstract class ErrorHelper
{
    public const string ValidationLayerName = "Validation";
    private const string PipeBaseId = "{C53C83CB-E71A-43EC-9D65-72CFAA3E02E8}";
    private const string SignalBaseId = "{AA964FAF-E393-47F1-AFC9-AD74613F595E}";

    /// <summary>
    ///     The designation for equipments should be unique within a functional group.
    ///     To help user locate the equipment with the wrong designation number, a mask will be place don the duplicated
    ///     equipments.
    /// </summary>
    /// <param name="page"></param>
    public static void CheckDesignationUnique(IVPage page)
    {
        var undoScope = Globals.ThisAddIn.Application.BeginUndoScope("Check Designation Unique");

        try
        {
            var duplicated = page.Shapes.OfType<Shape>()
                .Where(x => (x.HasCategory("Equipment") || x.HasCategory("Instrument")) &&
                            !string.IsNullOrEmpty(x.CellsU["Prop.FunctionalElement"]
                                .ResultStr[VisUnitCodes.visUnitsString]) &&
                            !string.IsNullOrEmpty(x.CellsU["Prop.FunctionalGroup"]
                                .ResultStr[VisUnitCodes.visUnitsString]))
                .Select(x => new
                {
                    x.ID,
                    FunctionalElement = x.CellsU["Prop.FunctionalElement"].TryGetFormatValue(),
                    FunctionalGroup = x.CellsU["Prop.FunctionalGroup"].ResultStr[VisUnitCodes.visUnitsString]
                })
                .GroupBy(x => new { x.FunctionalGroup, x.FunctionalElement })
                .Where(x => x.Count() != 1)
                .ToList();

            if (duplicated.Count != 0)
            {
                var validationLayer = EnsureEnvironment(page);

                foreach (var item in duplicated.SelectMany(x => x)) HighlightShape(page, item.ID, validationLayer);
            }
            else
            {
                MessageBox.Show("未发现异常。", "检查", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            Globals.ThisAddIn.Application.EndUndoScope(undoScope, true);
        }
        catch (Exception ex)
        {
            Globals.ThisAddIn.Application.EndUndoScope(undoScope, false);

            // log
            LogHost.Default.Error(ex, "Failed to check designation unique");

            // display error message
            MessageBox.Show(ex.Message, "检查失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }


    /// <summary>
    ///     Clear the masks on the validation layer.
    /// </summary>
    /// <param name="page"></param>
    public static void ClearCheckMarks(IVPage page)
    {
        var selection = page.CreateSelection(VisSelectionTypes.visSelTypeByLayer, VisSelectMode.visSelModeSkipSuper,
            ValidationLayerName);
        if (selection.Count > 0)
            selection.Delete();
    }

    public static void ScanMaster(Page page)
    {
        var undoScope = Globals.ThisAddIn.Application.BeginUndoScope("Scan Master Lost");

        try
        {
            var noMasters = page.Shapes.OfType<Shape>()
                .Where(x => x.CellExistsN("User.msvShapeCategories", VisExistsFlags.visExistsAnywhere))
                .Where(x => x.Master == null)
                .ToList();

            if (noMasters.Any())
            {
                var validationLayer = EnsureEnvironment(page);
                foreach (var item in noMasters) HighlightShape(page, item.ID, validationLayer);
            }
            else
            {
                MessageBox.Show("未发现异常。", "检查", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            Globals.ThisAddIn.Application.EndUndoScope(undoScope, true);
        }
        catch (Exception ex)
        {
            Globals.ThisAddIn.Application.EndUndoScope(undoScope, false);

            // log
            LogHost.Default.Error(ex, "Failed to scan master lost.");

            // display error message
            MessageBox.Show(ex.Message, "检查失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public static void ScanPipeline(Page page)
    {
        var undoScope = Globals.ThisAddIn.Application.BeginUndoScope("Scan Pipeline");

        try
        {
            var pipelines = page.Shapes.OfType<Shape>()
                .Where(x => x.Master != null && (x.Master.BaseID == PipeBaseId || x.Master.BaseID == SignalBaseId))
                .ToList();

            var validationLayer = EnsureEnvironment(page);

            var hasError = false;

            foreach (var pipeline in from pipeline in pipelines
                     where pipeline.OneD == (short)VBABool.True
                     select pipeline)
            {
                var id = pipeline.ID;
                var beginX = pipeline.CellsSRCN(VisSectionIndices.visSectionObject, VisRowIndices.visRowXForm1D,
                    VisCellIndices.vis1DBeginX).ResultIU;
                var beginY = pipeline.CellsSRCN(VisSectionIndices.visSectionObject, VisRowIndices.visRowXForm1D,
                    VisCellIndices.vis1DBeginY).ResultIU;

                // 2025.02.07: 必须使用FormulaU去获取公式，否则可能因为区域问题导致比较时与预想的不一样。本例中如果使用Formula，在有的电脑上会认为0 mm不是0 mm。
                var formulaBeginX = pipeline.CellsSRCN(VisSectionIndices.visSectionObject,
                    VisRowIndices.visRowXForm1D,
                    VisCellIndices.vis1DBeginX).FormulaU;
                var formulaBeginY = pipeline.CellsSRCN(VisSectionIndices.visSectionObject,
                    VisRowIndices.visRowXForm1D,
                    VisCellIndices.vis1DBeginY).FormulaU;

                if (formulaBeginX == "0 mm" && formulaBeginY == "0 mm")
                {
                    hasError = true;
                    HighlightShape(page, pipeline.ID, validationLayer);
                }
            }

            // 2025.02.07： 增加一个用户提示告诉用户没有异常
            if (!hasError) MessageBox.Show("未发现异常。", "检查", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Globals.ThisAddIn.Application.EndUndoScope(undoScope, true);
        }
        catch (Exception ex)
        {
            Globals.ThisAddIn.Application.EndUndoScope(undoScope, false);

            // log
            LogHost.Default.Error(ex, "Failed to scan pipeline.");

            // display error message
            MessageBox.Show(ex.Message, "检查失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static Layer EnsureEnvironment(IVPage page)
    {
        // create validation layer if not exist
        var validationLayer =
            page.Layers.OfType<Layer>().SingleOrDefault(x => x.Name == ValidationLayerName) ??
            page.Layers.Add(ValidationLayerName);
        validationLayer.CellsC[2].FormulaU = "2"; // set layer color
        validationLayer.CellsC[11].FormulaU = "50%"; // set layer transparency
        ClearCheckMarks(page);
        return validationLayer;
    }

    private static void HighlightShape(IVPage page, int id, Layer validationLayer)
    {
        var (left, bottom, right, top) = page.Shapes.ItemFromID[id]
            .BoundingBoxMetric((short)VisBoundingBoxArgs.visBBoxDrawingCoords +
                               (short)VisBoundingBoxArgs.visBBoxExtents);
        var rect = page.DrawRectangleMetric(left - 1, bottom - 1, right + 1, top + 1);
        // set as transparent fill
        rect.CellsSRCN(VisSectionIndices.visSectionObject, VisRowIndices.visRowFill, VisCellIndices.visFillPattern)
            .FormulaU = "9";
        // set layer
        rect.CellsSRCN(VisSectionIndices.visSectionObject, VisRowIndices.visRowLayerMem,
                VisCellIndices.visLayerMember).FormulaU = $"\"{validationLayer.Index - 1}\"";
    }
}