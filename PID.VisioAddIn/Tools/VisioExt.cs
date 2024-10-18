using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using AE.PID.Interfaces;
using AE.PID.Models;
using Microsoft.Office.Interop.Visio;
using Splat;

namespace AE.PID.Tools;

internal static class VisioExt
{
    public static void SetValue(this Cell source, string value)
    {
        source.FormulaU = $"\"{value}\"";
    }

    public static void SetValue(this Cell source, double value)
    {
        source.FormulaU = value.ToString(CultureInfo.InvariantCulture);
    }

    public static void SetValue(this Cell source, int value)
    {
        source.FormulaU = value.ToString();
    }

    private static Row GetOrAdd(this IVShape shape, IProp prop)
    {
        var existsAnywhere = shape.CellExistsN(prop.FullName, VisExistsFlags.visExistsAnywhere);
        if (existsAnywhere) return shape.Cells[prop.FullName].ContainingRow;

        // if not exist, check if the section exist
        var rowIndex = shape.AddRow((short)prop.GetSectionIndices(), (short)VisRowIndices.visRowLast,
            (short)tagVisRowTags.visTagDefault);
        var row = shape.Section[(short)prop.GetSectionIndices()][rowIndex];
        row.NameU = prop.Name;

        LogHost.Default.Info($"[Row创建]{shape.Name}：{prop.FullName}");
        return row;
    }

    /// <summary>
    ///     Try to delete a property from the shape.
    /// </summary>
    /// <param name="shape"></param>
    /// <param name="fullName"></param>
    /// <returns></returns>
    public static bool TryDelete(this IVShape shape, string fullName)
    {
        var result = false;
        if (!shape.CellExistsN(fullName, VisExistsFlags.visExistsLocally))
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

    #region Get Methods

    private static string Truncate(string originalString, string formatPattern)
    {
        if (string.IsNullOrEmpty(originalString)) return originalString;

        var number = double.Parse(originalString);
        var decimalPlaces = formatPattern.Length - formatPattern.IndexOf('.') - 1;
        return number.ToString($"F{decimalPlaces}");
    }

    /// <summary>
    ///     Get formatted value of the property in shape sheet.
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    public static string TryGetFormatValue(this IVCell cell)
    {
        var row = cell.ContainingRow;
        return row.GetFormatString();
    }


    /// <summary>
    ///     Get the value of the row and return value mix format. if the row is not a Shape Data row, return origin value
    ///     instead.
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    private static string GetFormatString(this IVRow row)
    {
        // get string value of the row
        var value = row.CellU[VisCellIndices.visCustPropsValue].ResultStr[VisUnitCodes.visUnitsString]!;
        // if the row is not the Shape Data section row, it will not contain any format, so return it directly.
        if (row.ContainingSection.Index != (short)VisSectionIndices.visSectionProp) return value;

        // if it is the Shape Data section row, need to check if it need format
        var type = row.CellU[VisCellIndices.visCustPropsType].ResultStr[VisUnitCodes.visNoCast];
        var format = row.CellU[VisCellIndices.visCustPropsFormat].ResultStr[VisUnitCodes.visUnitsString];

        // if the row is a list or variable list, or it format is empty, return it directly
        if ((type != "0" && type != "2") || string.IsNullOrEmpty(format)) return value;

        var result = Regex.Replace(format, @"(\\.)|(@)|(0\.[#0]+)|(#\\)", match =>
        {
            if (match.Groups[1].Success)
                return match.Groups[1].Value.Substring(1); // Replace \\char with char

            if (match.Groups[2].Success)
                return value; // Replace @ with the original string

            if (match.Groups[3].Success)
                return Truncate(value, match.Value); // Handle other numeric patterns

            if (match.Groups[4].Success)
                return double.TryParse(value, out var number) ? number.ToString("F") : "0";

            return match.Value;
        });
        return result;
    }

    public static string? TryGetValue(this IVShape shape, string propName)
    {
        var existsAnywhere = shape.CellExistsN(propName, VisExistsFlags.visExistsAnywhere);
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
        var existsAnywhere = shape.CellExistsN(propName, VisExistsFlags.visExistsAnywhere);
        if (!existsAnywhere) return null;
        var row = shape.Cells[propName].ContainingRow;

        return row.GetFormatString();
    }

    #endregion

    #region Geo Helper

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

    #endregion

    #region Update Methods

    public static Row CreateOrUpdate(this IVShape shape, IProp prop)
    {
        if (prop is IActionData actionData)
            return shape.CreateOrUpdate(actionData);
        if (prop is IUserData userData)
            return shape.CreateOrUpdate(userData);
        if (prop is IShapeData shapeData)
            return shape.CreateOrUpdate(shapeData);

        throw new InvalidOperationException("Invalid property.");
    }

    public static Row CreateOrUpdate(this IVShape shape, IActionData data, bool ask = false)
    {
        var row = shape.GetOrAdd(data);

        row.CellU[VisCellIndices.visActionAction].UpdateIfChanged(data.Action, ask);
        row.CellU[VisCellIndices.visActionMenu].UpdateIfChanged(data.Menu, ask);
        row.CellU[VisCellIndices.visActionChecked].UpdateIfChanged(data.Checked, ask);
        row.CellU[VisCellIndices.visActionFlyoutChild].UpdateIfChanged(data.FlyoutChild, ask);

        return row;
    }

    public static Row CreateOrUpdate(this IVShape shape, IUserData data, bool ask = false)
    {
        var row = shape.GetOrAdd(data);

        row.CellU[VisCellIndices.visUserPrompt].UpdateIfChanged(data.Prompt, ask);
        row.CellU[VisCellIndices.visUserValue].UpdateIfChanged(data.DefaultValue, ask);

        return row;
    }

    public static Row CreateOrUpdate(this IVShape shape, IShapeData data, bool ask = false)
    {
        var row = shape.GetOrAdd(data);

        row.CellU[VisCellIndices.visCustPropsLabel].UpdateIfChanged(data.Label, ask);
        row.CellU[VisCellIndices.visCustPropsFormat].UpdateIfChanged(data.Format, ask);
        row.CellU[VisCellIndices.visCustPropsType].UpdateIfChanged(data.Type, ask);
        row.CellU[VisCellIndices.visCustPropsSortKey].UpdateIfChanged(data.SortKey, ask);
        row.CellU[VisCellIndices.visCustPropsInvis].UpdateIfChanged(data.Invisible, ask);
        row.CellU[VisCellIndices.visCustPropsValue].UpdateIfChanged(data.DefaultValue, ask);

        return row;
    }

    /// <summary>
    ///     Update a cell's value with new one. Prompt a dialog to let user choose whether to keep or discard the old one if
    ///     old one is not default value.
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="newValue"></param>
    /// <param name="ask"></param>
    /// <returns></returns>
    public static bool UpdateIfChanged(this IVCell cell, string newValue, bool ask = false)
    {
        var oldFormula = cell.FormulaU;
        var newFormula = $"\"{newValue}\"";

        if (cell.Section == (short)VisSectionIndices.visSectionProp)
        {
            var format = cell.ContainingRow.CellU[VisCellIndices.visCustPropsType]
                .ResultStr[VisUnitCodes.visUnitsString];
            if (format == "2") newFormula = $"{newValue}";
        }

        if (oldFormula == newFormula) return false;

        // if cell value already exist
        if (ask && oldFormula != "0" && oldFormula != "\"\"")
            if (MessageBox.Show(
                    $"属性 {cell.Name} 对于形状 {cell.Shape.Name} 已经有用户定义的值：{oldFormula}。您是否希望使用新值 {newValue} 覆盖它？{Environment.NewLine + Environment.NewLine}点击 '是' 覆盖该值，或点击 '否' 保留当前值。",
                    @"属性值覆盖确认",
                    MessageBoxButtons.YesNo) == DialogResult.No)
                return false;
        cell.FormulaU = newFormula;

        return true;
    }

    #endregion
}