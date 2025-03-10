using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AE.PID.Client.Core;
using Microsoft.Office.Interop.Visio;
using Splat;
using Shape = Microsoft.Office.Interop.Visio.Shape;

namespace AE.PID.Client.VisioAddIn;

public abstract class LegendHelper
{
    private const int Columns = 4;
    private const int RowSpacing = 10;
    private const int ColSpacing = 240 / Columns;

    public static void Insert(IVPage page)
    {
        Globals.ThisAddIn.Application.ShowChanges = false;
        var undoScope = page.Application.BeginUndoScope("Insert Legend");

        try
        {
            // open built in stencil to allow callout that used as legend item label
            Globals.ThisAddIn.Application.Documents.OpenEx(
                Globals.ThisAddIn.Application.GetBuiltInStencilFile(VisBuiltInStencilTypes.visBuiltInStencilCallouts,
                    VisMeasurementSystem.visMSMetric),
                (short)VisOpenSaveArgs.visOpenDocked + (short)VisOpenSaveArgs.visAddHidden);

            var items = PopulateLegendItems(page);

            var layer = EnsureLegendLayerExist(page);

            var basePosition = GetBasePoint(page);
            var rows = (int)Math.Ceiling((double)items.Count / Columns);

            DropLegendFrame(page, basePosition, rows);
            DropLegendItems(page, items, rows, basePosition, layer);

            page.Application.EndUndoScope(undoScope, true);
        }
        catch (Exception ex)
        {
            page.Application.EndUndoScope(undoScope, false);

            // log
            LogHost.Default.Error(ex, "Failed to generate legend.");

            // display error message
            MessageBox.Show(ex.Message, "生成图例失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Globals.ThisAddIn.Application.ShowChanges = true;
        }
    }

    private static void DropLegendItems(IVPage page, List<LegendItem> items, int rows, (double, double) basePosition,
        IVLayer layer)
    {
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];

            // compute the next insert position
            // ReSharper disable once PossibleLossOfFraction
            var (rowIndex, colIndex) = ComputeIndex(i, rows);
            var xPos = colIndex * ColSpacing + basePosition.Item1 + 5;
            var yPos = rowIndex * RowSpacing + basePosition.Item2 + 5;

            var shape = page.DropMetric(item.Source.Master, (basePosition.Item1, basePosition.Item2));

            // correct the properties, legend item should have count 0
            // because the subclass value might be a derived result from many other cells, if directly copy the value, the shape will not change as expect
            // so firstly check if it is a derived result.
            SetRecursively(shape.CellsU[CellDict.SubClass], item.Source);

            shape.CellsU[CellDict.UnitQuantity].FormulaForce = "0";

            // replace the category to legend
            if (!shape.HasCategory("Legend"))
                shape.CellsU[CellDict.ShapeCategories].FormulaForce = "\"Legend\"";

            ResizeAtPin(shape);
            ReLocateToGeometricCenter(shape, xPos, yPos);

            // add a label
            var label = InsertLabelAsCallout(shape);

            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLayerMem,
                    (short)VisCellIndices.visLayerMember].FormulaForceU = $"\"{layer.Index}\"";
            label.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLayerMem,
                    (short)VisCellIndices.visLayerMember].FormulaForceU = $"\"{layer.Index}\"";

            shape.AddToContainers();
        }
    }

    private static IVLayer EnsureLegendLayerExist(IVPage page)
    {
        var layer = page.Layers.OfType<IVLayer>().SingleOrDefault(x => x.Name == LayerDict.Legends) ??
                    page.Layers.Add(LayerDict.Legends);
        return layer;
    }

    private static void DropLegendFrame(IVPage page, (double X, double Y) basePosition, int rows)
    {
        var container = page.DrawRectangleMetric(basePosition.X, basePosition.Y, basePosition.X + 240,
            basePosition.Y + rows * 10);
        container.AddSection((short)VisSectionIndices.visSectionUser);
        container.AddRow((short)VisSectionIndices.visSectionUser, (short)VisRowIndices.visRowLast,
            (short)VisRowTags.visTagDefault);

        container.CellsSRC[(short)VisSectionIndices.visSectionUser, 0, (short)VisCellIndices.visUserValue]
            .RowName = "msvStructureType";
        container.CellsSRC[(short)VisSectionIndices.visSectionUser, 0, (short)VisCellIndices.visUserValue]
            .FormulaU = "\"Container\"";
        container.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLine,
            (short)VisCellIndices.visLineWeight].FormulaU = "0.6 mm";

        // 2025.01.28: add title for the legend
        // 2025.02.10: adjust title height to 12 and set font type to 等线
        var title = page.DrawRectangleMetric(basePosition.X, basePosition.Y + rows * 10,
            basePosition.X + 240,
            basePosition.Y + rows * 10 + 12);
        title.Text = "图例\nLegend";
        title.Characters.CharProps[(short)VisCellIndices.visCharacterAsianFont] = 313;
        title.Characters.CharProps[(short)VisCellIndices.visCharacterFont] = 313;
        title.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLine,
            (short)VisCellIndices.visLineWeight].FormulaU = "0.6 mm";
    }

    private static (int, int) ComputeIndex(int i, int rows)
    {
        var rowIndex = i % rows;
        var colIndex = i / rows;
        return (rowIndex, colIndex);
    }

    private static void SetRecursively(Cell cell, Shape source)
    {
        var precedents = cell.Precedents;
        if (precedents == null || precedents.Length == 0 || precedents.OfType<IVCell>().First().RowName == "SubClass")
            cell.FormulaForceU = source.CellsU[cell.Name].FormulaU;
        else
            foreach (Cell precedentCell in precedents)
            {
                if (precedentCell.Row == cell.Row) continue;
                SetRecursively(precedentCell, source);
            }
    }

    private static (double, double) GetBasePoint(IVPage page)
    {
        // if there is a frame, get the top left corner of the block title
        var frame = page.Shapes.OfType<Shape>()
            .FirstOrDefault(x => x.Master?.BaseID == BaseIdDict.Frame);
        if (frame != null)
        {
            var (_, bottom, right, _) = frame.BoundingBoxMetric((short)VisBoundingBoxArgs.visBBoxDrawingCoords +
                                                                (short)VisBoundingBoxArgs.visBBoxExtents);
            return new ValueTuple<double, double>(right - 250, bottom + 52);
        }

        // if not, get the center screen
        page.Application.ActiveWindow.GetViewRect(out var pdLeft, out var pdTop, out var pdWidth, out var pdHeight);
        return new ValueTuple<double, double>((pdWidth / 2 + pdLeft) * 25.4, (pdTop - pdHeight / 2) * 25.4);
    }

    private static List<LegendItem> PopulateLegendItems(IVPage page)
    {
        return
        [
            .. page.Shapes.OfType<Shape>()
                .Where(x => x.Master != null)
                .Where(x => !x.HasCategory("Proxy") && (x.HasCategory("Equipment") || x.HasCategory("Equipment") ||
                                                        x.HasCategory("Instrument") || x.HasCategory("Equipments")))
                .GroupBy(x => new
                {
                    Class = x.CellsU[CellDict.Class].ResultStr[tagVisUnitCodes.visUnitsString],
                    SubClassName = x.CellsU[CellDict.SubClass].ResultStr[tagVisUnitCodes.visUnitsString]
                })
                .Select(x => new LegendItem(x.Key.Class, x.Key.SubClassName, x.First()))
                .OrderBy(x => x.Category)
                .ThenBy(x => x.SubclassName)
        ];
    }

    private static void ReLocateToGeometricCenter(IVShape shape, double xPos, double yPos)
    {
        shape.CellsU["PinX"].FormulaU = $"{xPos} mm";
        shape.CellsU["PinY"].FormulaU = $"{yPos} mm";

        // after scaled, we should get the displacement between the center of the nominal bounding and bbox bounding, so that we could place the bbox center at the target position
        var alignBoxCenter = shape.GetPinLocation();
        var geoCenter = shape.GetGeometricCenter();

        var displacement = Math.Round(geoCenter.Item2 - alignBoxCenter.Item2, 4);
        shape.CellsU["PinY"].FormulaU = $"{alignBoxCenter.Item2 - displacement} mm";
    }

    private static void ResizeAtPin(IVShape shape)
    {
        // resize
        var nominalWidth = shape.CellsU["Width"].Result["mm"];
        var nominalHeight = shape.CellsU["Height"].Result["mm"];

        // get the bbox extent, which is the smallest bounding box that surround the visible geometry.
        // pay attention that the flag is visBBoxDrawingCoords + visBBoxExtents
        var (left, bottom, right, top) = shape.BoundingBoxMetric((short)VisBoundingBoxArgs.visBBoxDrawingCoords +
                                                                 (short)VisBoundingBoxArgs.visBBoxExtents);
        var actualWidth = Math.Round(right - left, 4);
        var actualHeight = Math.Round(top - bottom, 4);

        // Now the nominal height from height property represents an actual height of actualHeight.
        // What we want is the actual height adjust to 5 mm, so we should scale the nominal height with [NH] / [DNH] = [AH] / [DAH], where DAH = 5 mm
        var desiredNominalHeight = Math.Round(actualWidth >= actualHeight
            ? nominalHeight / actualWidth * 5
            : nominalHeight / actualHeight * 5);
        var desiredNominalWidth = Math.Round(actualWidth >= actualHeight
            ? nominalWidth / actualWidth * 5
            : nominalWidth / actualHeight * 5);

        shape.CellsU["Width"].FormulaForceU = $"GUARD({desiredNominalWidth} mm)";
        shape.CellsU["Height"].FormulaForceU = $"GUARD({desiredNominalHeight} mm)";
    }

    private static Shape InsertLabelAsCallout(Shape shape)
    {
        var calloutDoc = Globals.ThisAddIn.Application.Documents[Globals.ThisAddIn.Application.GetBuiltInStencilFile(
            VisBuiltInStencilTypes.visBuiltInStencilCallouts,
            VisMeasurementSystem.visMSMetric)];
        var callout = shape.ContainingPage.DropCallout(calloutDoc.Masters.ItemU["Text Callout"], shape);

        callout.CellsU["LocPinX"].FormulaU = "Width*0";
        callout.CellsU["PinX"].FormulaU = $"{shape.CellsU["PinX"].Result["mm"] + 5} mm";

        var (_, extentsBottom, _, extentsTop) = shape.BoundingBoxMetric(
            (short)VisBoundingBoxArgs.visBBoxDrawingCoords + (short)VisBoundingBoxArgs.visBBoxExtents);
        callout.CellsU["PinY"].FormulaU = $"{Math.Round((extentsBottom + extentsTop) / 2)} mm";

        callout.DeleteSection((short)VisSectionIndices.visSectionFirstComponent + 1);
        callout.DeleteSection((short)VisSectionIndices.visSectionFirstComponent);

        callout.Characters.AddCustomFieldU($"Sheet.{shape.ID}!{CellDict.SubClass}",
            (short)VisFieldFormats.visFmtNumGenNoUnits);

        return callout;
    }

    private class LegendItem(string category, string subclassName, Shape source)
    {
        public string Category { get; } = category;
        public string SubclassName { get; } = subclassName;
        public Shape Source { get; } = source;
    }
}