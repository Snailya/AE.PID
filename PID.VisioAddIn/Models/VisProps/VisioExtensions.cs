using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using AE.PID.Controllers.Services;
using AE.PID.Interfaces;
using AE.PID.Models.BOM;
using AE.PID.Models.Exceptions;
using AE.PID.Properties;
using Microsoft.Office.Interop.Visio;
using NLog;

namespace AE.PID.Models.VisProps;

internal static class VisioExtension
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static Row GetOrAdd(this IVShape shape, IProp prop)
    {
        var existsAnywhere = shape.CellExists[prop.FullName, (short)VisExistsFlags.visExistsAnywhere] ==
                             (short)VBABool.True;
        if (existsAnywhere) return shape.Cells[prop.FullName].ContainingRow;

        // if not exist, check if the section exist
        var rowIndex = shape.AddRow((short)prop.GetSectionIndices(), (short)VisRowIndices.visRowLast,
            (short)tagVisRowTags.visTagDefault);
        var row = shape.Section[(short)prop.GetSectionIndices()][rowIndex];
        row.NameU = prop.Name;

        //ThisAddIn.Logger.Information("[Row创建]{ShapeName}：{RowName}", shape.Name, prop.FullName);
        return row;
    }

    public static Row AddOrUpdate(this IVShape shape, IActionData data)
    {
        var row = shape.GetOrAdd(data);

        row.CellU[VisCellIndices.visActionAction].Update(data.Action);
        row.CellU[VisCellIndices.visActionMenu].Update(data.Menu);
        row.CellU[VisCellIndices.visActionChecked].Update(data.Checked);
        row.CellU[VisCellIndices.visActionFlyoutChild].Update(data.FlyoutChild);

        return row;
    }

    public static Row AddOrUpdate(this IVShape shape, IUserData data)
    {
        var row = shape.GetOrAdd(data);

        row.CellU[VisCellIndices.visUserPrompt].Update(data.Prompt);
        row.CellU[VisCellIndices.visUserValue].Update(data.DefaultValue);

        return row;
    }

    public static Row AddOrUpdate(this IVShape shape, IShapeData data)
    {
        var row = shape.GetOrAdd(data);

        row.CellU[VisCellIndices.visCustPropsLabel].Update(data.Label);
        row.CellU[VisCellIndices.visCustPropsFormat].Update(data.Format);
        row.CellU[VisCellIndices.visCustPropsType].Update(data.Type);
        row.CellU[VisCellIndices.visCustPropsSortKey].Update(data.SortKey);
        row.CellU[VisCellIndices.visCustPropsInvis].Update(data.Invisible);
        row.CellU[VisCellIndices.visCustPropsValue].Update(data.DefaultValue);

        return row;
    }

    /// <summary>
    ///     Update a cell's value with new one. Prompt a dialog to let user choose whether to keep or discard the old one if
    ///     old one is not default value.
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="newValue"></param>
    /// <param name="force"></param>
    /// <returns></returns>
    public static bool Update(this IVCell cell, string newValue, bool force = false)
    {
        var oldValue = cell.FormulaU;
        if (string.IsNullOrEmpty(newValue) || oldValue == newValue) return false;

        // if cell value already exist
        if (!force && oldValue != "0" && oldValue != "\"\"")
            if (MessageBox.Show(string.Format(Resources.Property_Value_Override_Confirmation, cell.Name,
                        cell.Shape.Name,
                        oldValue, newValue, Environment.NewLine + Environment.NewLine), @"属性值覆盖确认",
                    MessageBoxButtons.YesNo) == DialogResult.No)
                return false;
        cell.FormulaForceU = newValue;

        return true;
    }

    /// <summary>
    ///     Try delete a property from the shape.
    /// </summary>
    /// <param name="shape"></param>
    /// <param name="fullName"></param>
    /// <returns></returns>
    public static bool TryDelete(this IVShape shape, string fullName)
    {
        var result = false;
        if (shape.CellExists[fullName, (short)VisExistsFlags.visExistsLocally] !=
            (short)VBABool.True)
            return false;

        var cell = shape.Cells[fullName];
        if (cell.Dependents.Length > 0)
        {
        }
        else
        {
            shape.DeleteRow(cell.Section, cell.Row);
            result = true;
        }

        return result;
    }

    /// <summary>
    /// Get formatted value from Shape Data. 
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    /// <exception cref="FormatValueInvalidException"></exception>
    public static string? GetFormatValue(this IVRow row)
    {
        try
        {
            var value = row.CellU[VisCellIndices.visCustPropsValue].ResultStr[VisUnitCodes.visUnitsString];
            if (string.IsNullOrEmpty(value)) return value;

            var type = row.CellU[VisCellIndices.visCustPropsType].ResultStr[VisUnitCodes.visNoCast];

            if (type != "0" && type != "2") return value;
            var format = row.CellU[VisCellIndices.visCustPropsFormat].ResultStr[VisUnitCodes.visUnitsString];
            if (string.IsNullOrEmpty(format)) return value;

            var result = Regex.Replace(format, @"(\\.)|(@)|(0\.[#0]+|#)", match =>
            {
                if (match.Groups[1].Success)
                    return match.Groups[1].Value.Substring(1); // Replace \\char with char

                if (match.Groups[2].Success)
                    return value; // Replace @ with the original string

                if (match.Groups[3].Success)
                    return Truncate(value, match.Value); // Handle other numeric patterns

                return match.Value;
            });
            return result;
        }
        catch (COMException comException)
        {
            Logger.Error(comException, $"Failed to get value form {row.Shape.ID}!{row.Name}.");
            throw new FormatValueInvalidException(row.Shape.ID, row.Name);
        }
    }

    /// <summary>
    /// Check if the shape is on specified layer.
    /// </summary>
    /// <param name="shape"></param>
    /// <param name="layerNames"></param>
    /// <returns></returns>
    public static bool IsOnLayers(this IVShape shape, IEnumerable<string> layerNames)
    {
        var enumerable = layerNames as string[] ?? layerNames.ToArray();

        for (short i = 1; i < shape.LayerCount + 1; i++)
        {
            var layer = shape.Layer[i];
            if (enumerable.Contains(layer.Name)) return true;
        }

        return false;
    }

    /// <summary>
    /// Drop an object using mm unit.
    /// </summary>
    /// <param name="page"></param>
    /// <param name="objectToDrop"></param>
    /// <param name="xPos"></param>
    /// <param name="yPos"></param>
    /// <returns></returns>
    public static Shape DropMetric(this IVPage page, object objectToDrop, double xPos, double yPos)
    {
        return page.Drop(objectToDrop, xPos / 25.4, yPos / 25.4);
    }

    public static Shape DrawRectangleMetric(this IVPage page, double x1, double y1, double x2, double y2)
    {
        return page.DrawRectangle(x1 / 25.4, y1 / 25.4, x2 / 25.4, y2 / 25.4);
    }

    /// <summary>
    /// Get bounding box in mm unit.
    /// </summary>
    /// <param name="shape"></param>
    /// <param name="flags"></param>
    /// <returns></returns>
    public static (double, double, double, double) BoundingBoxMetric(this IVShape shape, short flags)
    {
        shape.BoundingBox(flags, out var left, out var bottom, out var right, out var top);
        return (left * 25.4, bottom * 25.4, right * 25.4, top * 25.4);
    }

    /// <summary>
    /// Get the pin location of the shape. The pin location is by default the align box's center
    /// </summary>
    /// <param name="shape"></param>
    /// <returns></returns>
    public static Position GetPinLocation(this IVShape shape)
    {
        return new Position(shape.CellsU["PinX"].Result["mm"], shape.CellsU["PinY"].Result["mm"]);
    }

    /// <summary>
    /// Get the geometric center of the shape. This is done by compute the center of BBox Extents.
    /// </summary>
    /// <param name="shape"></param>
    /// <returns></returns>
    public static Position GetGeometricCenter(this IVShape shape)
    {
        var (left, bottom, right, top) = shape.BoundingBoxMetric(
            (short)VisBoundingBoxArgs.visBBoxDrawingCoords + (short)VisBoundingBoxArgs.visBBoxExtents);
        return new Position(left + right / 2, (top + bottom) / 2);
    }

    /// <summary>
    /// Try convert a shape to line item.
    /// </summary>
    /// <param name="shape"></param>
    /// <returns></returns>
    public static Element? ToElement(this IVShape shape)
    {
        try
        {
            // general properties
            var item = new Element
            {
                Id = shape.ID,
                ProcessZone = shape.Cells["Prop.ProcessZone"].ResultStr[VisUnitCodes.visUnitsString],
                FunctionalGroup = shape.Cells["Prop.FunctionalGroup"].ResultStr[VisUnitCodes.visUnitsString],
                FunctionalElement = TryGetFormatValue(shape, "Prop.FunctionalElement") ?? string.Empty
            };

            // get D_BOM if exist
            if (shape.CellExists["Prop.D_BOM", (short)VisExistsFlags.visExistsLocally] == (short)VBABool.True)
                item.MaterialNo = shape.Cells["Prop.D_BOM"].ResultStr[VisUnitCodes.visUnitsString];

            // if it is a container, check if it is a unit container
            if (shape.ContainerProperties != null)
            {
                // if it is not a unit but other containers return null
                if (shape.CellExists["Prop.UnitName", (short)VisExistsFlags.visExistsAnywhere] !=
                    (short)VBABool.True) return null;

                item.Name = shape.Cells["Prop.UnitName"].ResultStr[VisUnitCodes.visUnitsString];
                item.Type = ElementType.Unit;

                if (double.TryParse(shape.Cells["Prop.Quantity"].ResultStr[VisUnitCodes.visUnitsString],
                        out var quantity))
                    item.Count = quantity;

                return item;
            }

            // if it is not a unit
            if (double.TryParse(shape.Cells["Prop.Subtotal"].ResultStr[VisUnitCodes.visUnitsString], out var subtotal))
                item.Count = subtotal;

            // if it is a single equipment
            if (shape.CellExists[LinkedControlManager.LinkedShapePropertyName,
                    (short)VisExistsFlags.visExistsAnywhere] !=
                (short)VBABool.True)
            {
                item.Name = shape.Cells["Prop.SubClass"].ResultStr[VisUnitCodes.visUnitsString];
                item.Type = ElementType.Single;

                // check if it has a parent
                var containers = shape.MemberOfContainers;
                if (containers.Length == 0) return item;

                // loop to find the unit id
                foreach (int containerId in containers)
                {
                    var parent = shape.ContainingPage.Shapes.ItemFromID[containerId];
                    if (parent.HasCategory("Unit"))
                        item.ParentId = containerId;
                }

                return item;
            }

            // if it is a attached equipment
            item.Name = shape.Cells["Prop.Name"].ResultStr[VisUnitCodes.visUnitsString];
            var parentId = (int)shape.CellsU[LinkedControlManager.LinkedShapePropertyName].ResultIU;
            item.ParentId = parentId;
            item.Type = ElementType.Attached;
            return item;
        }
        catch (Exception e)
        {
            Logger.Warn(e, $"Failed to convert ID:{shape.ID} to an Element, please check if shape is a valid AE item.");
            return null;
        }
    }

    private static string? TryGetFormatValue(IVShape shape, string propName)
    {
        var existsAnywhere = shape.CellExists[propName, (short)VisExistsFlags.visExistsAnywhere] ==
                             (short)VBABool.True;
        if (!existsAnywhere) return null;
        var row = shape.Cells[propName].ContainingRow;

        return row.GetFormatValue();
    }

    private static string? Truncate(string? originalString, string formatPattern)
    {
        if (!formatPattern.Contains(".") || !char.IsDigit(originalString[0])) return originalString;
        var decimalIndex = originalString.IndexOf('.');
        return originalString.Substring(0, decimalIndex);
    }
}