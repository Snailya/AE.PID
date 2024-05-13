using System;
using System.Collections.Generic;
using System.Linq;
using AE.PID.Models;
using AE.PID.Tools;
using Microsoft.Office.Interop.Visio;
using Splat;

namespace AE.PID.Services;

public class LegendGenerator(IVPage page) : IEnableLogger
{
    private const int Columns = 3;
    private const int RowSpacing = 10;
    private const int ColSpacing = 180 / Columns;

    public void Insert()
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

            // add new layer if not exist
            var legendsLayer = page.Layers.OfType<IVLayer>().SingleOrDefault(x => x.Name == "Legends") ??
                               page.Layers.Add("Legends");

            // loop to get all shapes with different subclass
            var legendItems = GetLegendItemsOnPage();

            // get the center of the screen as the base point
            page.Application.ActiveWindow.GetViewRect(out var pdLeft, out var pdTop, out var pdWidth, out var pdHeight);
            var centerScreen = new Position((pdWidth / 2 + pdLeft) * 25.4, (pdTop - pdHeight / 2) * 25.4);

            var rows = (int)Math.Ceiling((double)legendItems.Count / Columns);
            var basePosition = new Position(centerScreen.X - 90, centerScreen.Y - rows * 5);

            var container = page.DrawRectangleMetric(basePosition.X, basePosition.Y, basePosition.X + 180,
                basePosition.Y + rows * 10);
            container.AddSection((short)VisSectionIndices.visSectionUser);
            container.AddRow((short)VisSectionIndices.visSectionUser, (short)VisRowIndices.visRowLast,
                (short)VisRowTags.visTagDefault);
            container.CellsSRC[(short)VisSectionIndices.visSectionUser, 0, (short)VisCellIndices.visUserValue]
                .RowName = "msvStructureType";
            container.CellsSRC[(short)VisSectionIndices.visSectionUser, 0, (short)VisCellIndices.visUserValue]
                .FormulaU = "\"Container\"";

            for (var i = 0; i < legendItems.Count; i++)
            {
                // compute the next insert position
                // ReSharper disable once PossibleLossOfFraction
                var (rowIndex, colIndex) = ComputeIndex(i, rows);
                var xPos = colIndex * ColSpacing + basePosition.X + 5;
                var yPos = rowIndex * RowSpacing + basePosition.Y + 5;

                var shape = page.DropMetric(legendItems[i].Shape, 0, 0);

                RemoveUnnecessaryShapeSheetData(shape);
                ResizeAtPin(shape);
                ReLocateToGeometricCenter(shape, xPos, yPos);

                // add a label
                var label = InsertLabelAsCallout(shape);

                shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLayerMem,
                        (short)VisCellIndices.visLayerMember].FormulaForceU = $"\"{legendsLayer.Index}\"";
                label.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLayerMem,
                        (short)VisCellIndices.visLayerMember].FormulaForceU = $"\"{legendsLayer.Index}\"";

                shape.AddToContainers();
            }

            page.Application.EndUndoScope(undoScope, true);
        }
        catch (Exception ex)
        {
            page.Application.EndUndoScope(undoScope, false);

            this.Log().Error(ex);
        }
        finally
        {
            Globals.ThisAddIn.Application.ShowChanges = true;
        }
    }

    private static (int, int) ComputeIndex(int i, int rows)
    {
        var rowIndex = i % rows;
        var colIndex = i / rows;
        return (rowIndex, colIndex);
    }

    private List<LegendItem> GetLegendItemsOnPage()
    {
        return
        [
            .. page.Shapes.OfType<Shape>()
                .Where(x => !x.HasCategory("Proxy") && (x.HasCategory("Equipment") || x.HasCategory("Instrument")))
                .Select(x =>
                    new LegendItem(x.CellsU["Prop.Class"].ResultStr[""], x.CellsU["Prop.SubClass"].ResultStr[""], x))
                .Distinct(new LegendItemComparer()).OrderBy(x => x.Category).ThenBy(x => x.Name)
        ];
    }

    private static void ReLocateToGeometricCenter(IVShape shape, double xPos, double yPos)
    {
        shape.CellsU["PinX"].Formula = $"{xPos} mm";
        shape.CellsU["PinY"].Formula = $"{yPos} mm";

        // after scaled, we should get the displacement between the center of the nominal bounding and bbox bounding, so that we could place the bbox center at the target position
        var alignBoxCenter = shape.GetPinLocation();
        var geoCenter = shape.GetGeometricCenter();

        var displacement = Math.Round(geoCenter.Y - alignBoxCenter.Y, 4);
        shape.CellsU["PinY"].Formula = $"{alignBoxCenter.Y - displacement} mm";
    }

    private static void ResizeAtPin(IVShape shape)
    {
        // resize
        var nominalWidth = shape.CellsU["Width"].Result["mm"];
        var nominalHeight = shape.CellsU["Height"].Result["mm"];

        // get the bbox extent, which is the smallest bounding box that surround the visible geometry.
        // pay attention that the flags is visBBoxDrawingCoords + visBBoxExtents
        var (left, bottom, right, top) = shape.BoundingBoxMetric((short)VisBoundingBoxArgs.visBBoxDrawingCoords +
                                                                 (short)VisBoundingBoxArgs.visBBoxExtents);
        var actualWidth = Math.Round(right - left, 4);
        var actualHeight = Math.Round(top - bottom, 4);

        // Now the nominal height from height property represents a actual height of actualHeight.
        // What we want is the actual height adjust to 5 mm, so we should scale the nominal height with [NH] / [DNH] = [AH] / [DAH], where DAH = 5 mm
        var desiredNominalHeight = Math.Round(actualWidth >= actualHeight
            ? nominalHeight / actualWidth * 5
            : nominalHeight / actualHeight * 5);
        var desiredNominalWidth = Math.Round(actualWidth >= actualHeight
            ? nominalWidth / actualWidth * 5
            : nominalWidth / actualHeight * 5);

        shape.CellsU["Width"].Formula = $"GUARD({desiredNominalWidth} mm)";
        shape.CellsU["Height"].Formula = $"GUARD({desiredNominalHeight} mm)";
    }

    private static void RemoveUnnecessaryShapeSheetData(IVShape shape)
    {
        // rotate back as the stencil
        shape.Cells["Angle"].Formula = "0 deg";

        // disable key parameter display
        if (shape.CellExistsN("User.ShowKeyParameters", VisExistsFlags.visExistsAnywhere))
            shape.CellsU["User.ShowKeyParameters"].Formula = "FALSE";

        // disable tag display
        if (shape.CellExistsN("Prop.Tag", VisExistsFlags.visExistsAnywhere))
            shape.CellsU["Prop.Tag"].FormulaForce = "\"\"";

        // make it not count for export
        if (shape.CellExistsN("Prop.Quantity", VisExistsFlags.visExistsAnywhere))
            shape.CellsU["Prop.Quantity"].Formula = "0";

        if (shape.CellExistsN("User.NumOfShapes", VisExistsFlags.visExistsLocally))
            shape.Cells["User.NumOfShapes"].Formula = "1";

        if (shape.CellExistsN("Prop.ValveIsAdjustable", VisExistsFlags.visExistsLocally))
            shape.Cells["Prop.ValveIsAdjustable"].Formula = "FALSE";
    }

    private static Shape InsertLabelAsCallout(Shape shape)
    {
        var calloutDoc = Globals.ThisAddIn.Application.Documents[Globals.ThisAddIn.Application.GetBuiltInStencilFile(
            VisBuiltInStencilTypes.visBuiltInStencilCallouts,
            VisMeasurementSystem.visMSMetric)];
        var callout = shape.ContainingPage.DropCallout(calloutDoc.Masters.ItemU["Text Callout"], shape);

        callout.CellsU["LocPinX"].Formula = "Width*0";
        callout.CellsU["PinX"].Formula = $"{shape.CellsU["PinX"].Result["mm"] + 5} mm";

        var (_, extentsBottom, _, extentsTop) = shape.BoundingBoxMetric(
            (short)VisBoundingBoxArgs.visBBoxDrawingCoords + (short)VisBoundingBoxArgs.visBBoxExtents);
        callout.CellsU["PinY"].Formula = $"{Math.Round((extentsBottom + extentsTop) / 2)} mm";

        callout.DeleteSection((short)VisSectionIndices.visSectionFirstComponent + 1);
        callout.DeleteSection((short)VisSectionIndices.visSectionFirstComponent);

        callout.Characters.AddCustomFieldU($"Sheet.{shape.ID}!Prop.SubClass",
            (short)VisFieldFormats.visFmtNumGenNoUnits);

        return callout;
    }

    private class LegendItem(string category, string name, IVShape shape)
    {
        public string Category { get; } = category;
        public string Name { get; } = name;
        public IVShape Shape { get; } = shape;
    }

    private class LegendItemComparer : IEqualityComparer<LegendItem>
    {
        public bool Equals(LegendItem x, LegendItem y)
        {
            return string.Equals(x.Category, y.Category, StringComparison.Ordinal) &&
                   string.Equals(x.Name, y.Name, StringComparison.Ordinal);
        }

        public int GetHashCode(LegendItem obj)
        {
            // Compute a hash code based on first and second names
            unchecked
            {
                var hash = 17;
                hash = hash * 23 + obj.Category?.GetHashCode() ?? 0;
                hash = hash * 23 + obj.Name?.GetHashCode() ?? 0;
                return hash;
            }
        }
    }
}