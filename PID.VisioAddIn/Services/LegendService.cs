using System;
using System.Collections.Generic;
using System.Linq;
using AE.PID.Models;
using AE.PID.Tools;
using Microsoft.Office.Interop.Visio;
using Splat;

namespace AE.PID.Services;

public class LegendService : IEnableLogger
{
    private const int Columns = 3;
    private const int RowSpacing = 10;
    private const int ColSpacing = 180 / Columns;

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

            // add new layer if not exist
            var legendsLayer = page.Layers.OfType<IVLayer>().SingleOrDefault(x => x.Name == "Legends") ??
                               page.Layers.Add("Legends");

            // loop to get all shapes with different subclass
            var legendItems = PopulateLegendItems(page);

            // get the center of the screen as the base point
            var basePosition = GetBasePoint(page);
            var rows = (int)Math.Ceiling((double)legendItems.Count / Columns);

            var container = page.DrawRectangleMetric(basePosition.X, basePosition.Y, basePosition.X + 180,
                basePosition.Y + rows * 10);
            container.AddSection((short)VisSectionIndices.visSectionUser);
            container.AddRow((short)VisSectionIndices.visSectionUser, (short)VisRowIndices.visRowLast,
                (short)VisRowTags.visTagDefault);

            container.CellsSRC[(short)VisSectionIndices.visSectionUser, 0, (short)VisCellIndices.visUserValue]
                .RowName = "msvStructureType";
            container.CellsSRC[(short)VisSectionIndices.visSectionUser, 0, (short)VisCellIndices.visUserValue]
                .FormulaU = "\"Container\"";
            container.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLine,
                (short)VisCellIndices.visLineWeight].FormulaU = "0.6mm";

            for (var i = 0; i < legendItems.Count; i++)
            {
                var item = legendItems[i];

                // compute the next insert position
                // ReSharper disable once PossibleLossOfFraction
                var (rowIndex, colIndex) = ComputeIndex(i, rows);
                var xPos = colIndex * ColSpacing + basePosition.X + 5;
                var yPos = rowIndex * RowSpacing + basePosition.Y + 5;

                var shape = page.DropMetric(item.Master, basePosition.X, basePosition.Y);
                shape.CellsU["Prop.SubClass"].FormulaForce = $"\"{item.SubclassName}\"";

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
        catch (Exception)
        {
            page.Application.EndUndoScope(undoScope, false);

            throw;
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

    private static Position GetBasePoint(IVPage page)
    {
        // if there is a frame, get the top left corner of the block title
        var frame = page.Shapes.OfType<Shape>()
            .FirstOrDefault(x => x.Master?.BaseID == Constants.FrameBaseId);
        if (frame != null)
        {
            var (_, bottom, right, _) = frame.BoundingBoxMetric((short)VisBoundingBoxArgs.visBBoxDrawingCoords +
                                                                (short)VisBoundingBoxArgs.visBBoxExtents);
            return new Position(right - 190, bottom + 52);
        }

        // if not, get the center screen
        page.Application.ActiveWindow.GetViewRect(out var pdLeft, out var pdTop, out var pdWidth, out var pdHeight);
        return new Position((pdWidth / 2 + pdLeft) * 25.4, (pdTop - pdHeight / 2) * 25.4);
    }

    private static List<LegendItem> PopulateLegendItems(IVPage page)
    {
        return
        [
            .. page.Shapes.OfType<Shape>()
                .Where(x => x.Master != null)
                .Where(x => !x.HasCategory("Proxy") && (x.HasCategory("Equipment") || x.HasCategory("Instrument")))
                .GroupBy(x => new
                {
                    Class = x.CellsU["Prop.Class"].ResultStr[tagVisUnitCodes.visUnitsString],
                    SubClassName = x.CellsU["Prop.SubClass"].ResultStr[tagVisUnitCodes.visUnitsString]
                })
                .Select(x => new LegendItem(x.Key.Class, x.Key.SubClassName, x.First().Master))
                .OrderBy(x => x.Category)
                .ThenBy(x => x.SubclassName)
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

        shape.CellsU["Width"].FormulaForce = $"GUARD({desiredNominalWidth} mm)";
        shape.CellsU["Height"].FormulaForce = $"GUARD({desiredNominalHeight} mm)";
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

    private class LegendItem(string category, string subclassName, Master master)
    {
        public string Category { get; } = category;
        public string SubclassName { get; } = subclassName;
        public IVMaster Master { get; } = master;
    }
}