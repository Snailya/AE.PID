using System.Linq;
using AE.PID.Visio.Extensions;
using Microsoft.Office.Interop.Visio;
using Page = Microsoft.Office.Interop.Visio.Page;
using Shape = Microsoft.Office.Interop.Visio.Shape;

namespace AE.PID.Visio.Helpers;

public abstract class ErrorHelper
{
    public static readonly string ValidationLayerName = "Validation";

    /// <summary>
    ///     The designation for equipments should be unique within a functional group.
    ///     To help user locate the equipment with the wrong designation number, a mask will be place don the duplicated
    ///     equipments.
    /// </summary>
    /// <param name="page"></param>
    public static void CheckDesignationUnique(IVPage page)
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

        if (duplicated.Count == 0) return;

        // create validation layer if not exist
        var validationLayer =
            page.Layers.OfType<Layer>().SingleOrDefault(x => x.Name == ValidationLayerName) ??
            page.Layers.Add(ValidationLayerName);
        validationLayer.CellsC[2].FormulaU = "2"; // set layer color
        validationLayer.CellsC[11].FormulaU = "50%"; // set layer transparency
        ClearCheckMarks(page);

        foreach (var item in duplicated.SelectMany(x => x))
        {
            var (left, bottom, right, top) = page.Shapes.ItemFromID[item.ID]
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
        var noMasters = page.Shapes.OfType<Shape>()
            .Where(x => x.CellExistsN("User.msvShapeCategories", VisExistsFlags.visExistsAnywhere))
            .Where(x => x.Master == null)
            .ToList();

        // create validation layer if not exist
        var validationLayer =
            page.Layers.OfType<Layer>().SingleOrDefault(x => x.Name == ValidationLayerName) ??
            page.Layers.Add(ValidationLayerName);
        validationLayer.CellsC[2].FormulaU = "2"; // set layer color
        validationLayer.CellsC[11].FormulaU = "50%"; // set layer transparency
        ClearCheckMarks(page);

        foreach (var item in noMasters)
        {
            var (left, bottom, right, top) = page.Shapes.ItemFromID[item.ID]
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
}