﻿using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using AE.PID.Interfaces;
using AE.PID.Models;
using AE.PID.Properties;
using Microsoft.Office.Interop.Visio;
using NLog;

namespace AE.PID.Tools;

internal static class VisioExtension
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static Row GetOrAdd(this IVShape shape, IProp prop)
    {
        var existsAnywhere = shape.CellExists[prop.FullName, (short)VisExistsFlags.visExistsAnywhere] ==
                             (short)VbaBool.True;
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
            (short)VbaBool.True)
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

    private static string? GetFormatValue(this IVRow row)
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
            return null;
        }
    }

    /// <summary>
    ///     Drop an object using mm unit.
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
    ///     Get bounding box in mm unit.
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
    ///     Get the pin location of the shape. The pin location is by default the align box's center
    /// </summary>
    /// <param name="shape"></param>
    /// <returns></returns>
    public static Position GetPinLocation(this IVShape shape)
    {
        return new Position(shape.CellsU["PinX"].Result["mm"], shape.CellsU["PinY"].Result["mm"]);
    }

    /// <summary>
    ///     Get the geometric center of the shape. This is done by compute the center of BBox Extents.
    /// </summary>
    /// <param name="shape"></param>
    /// <returns></returns>
    public static Position GetGeometricCenter(this IVShape shape)
    {
        var (left, bottom, right, top) = shape.BoundingBoxMetric(
            (short)VisBoundingBoxArgs.visBBoxDrawingCoords + (short)VisBoundingBoxArgs.visBBoxExtents);
        return new Position(left + right / 2, (top + bottom) / 2);
    }

    public static string? TryGetValue(this IVShape shape, string propName)
    {
        var existsAnywhere = shape.CellExists[propName, (short)VisExistsFlags.visExistsAnywhere] ==
                             (short)VbaBool.True;
        if (!existsAnywhere) return null;
        var cell = shape.Cells[propName];
        return cell.ResultStr[VisUnitCodes.visUnitsString];
    }

    /// <summary>
    ///     Get formatted value of the property in shape sheet.
    /// </summary>
    /// <param name="shape"></param>
    /// <param name="propName"></param>
    /// <returns></returns>
    public static string? TryGetFormatValue(this IVShape shape, string propName)
    {
        var existsAnywhere = shape.CellExists[propName, (short)VisExistsFlags.visExistsAnywhere] ==
                             (short)VbaBool.True;
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