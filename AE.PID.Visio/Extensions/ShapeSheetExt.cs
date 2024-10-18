using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Office.Interop.Visio;
using Splat;

namespace AE.PID.Visio.Extensions;

public static class ShapeSheetExt
{
    /// <summary>
    ///     Check if the cell with specified name exist.
    /// </summary>
    /// <param name="shape"></param>
    /// <param name="propName"></param>
    /// <param name="flags"></param>
    /// <returns></returns>
    public static bool CellExistsN(this IVShape shape, string propName, VisExistsFlags flags)
    {
        return shape.CellExists[propName, (short)flags] ==
               (short)VBABool.True;
    }

    /// <summary>
    ///     Get the cell by vis indices.
    /// </summary>
    /// <param name="shape"></param>
    /// <param name="sectionIndices"></param>
    /// <param name="rowIndices"></param>
    /// <param name="cellIndices"></param>
    /// <returns></returns>
    public static Cell CellsSRCN(this IVShape shape, VisSectionIndices sectionIndices, VisRowIndices rowIndices,
        VisCellIndices cellIndices)
    {
        return shape.CellsSRC[(short)sectionIndices, (short)rowIndices, (short)cellIndices];
    }


    /// <summary>
    ///     Try to get the format value of the cell.
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    public static string TryGetFormatValue(this IVCell cell)
    {
        var row = cell.ContainingRow;
        return row.GetFormatString();
    }

    /// <summary>
    ///     Try to get the format value by specify property name.
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

    /// <summary>
    ///     Try get value by specify property name.
    /// </summary>
    /// <param name="shape"></param>
    /// <param name="propName"></param>
    /// <returns></returns>
    public static string? TryGetValue(this IVShape shape, string propName)
    {
        var existsAnywhere = shape.CellExistsN(propName, VisExistsFlags.visExistsAnywhere);
        if (!existsAnywhere) return null;
        var cell = shape.Cells[propName];
        return cell.ResultStr[VisUnitCodes.visUnitsString];
    }

    private static string Truncate(string originalString, string formatPattern)
    {
        if (string.IsNullOrEmpty(originalString)) return originalString;

        var number = double.Parse(originalString);
        var decimalPlaces = formatPattern.Length - formatPattern.IndexOf('.') - 1;
        return number.ToString($"F{decimalPlaces}");
    }

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

    private static void CreateIfNotExist(this IVShape shape, string propName)
    {
        if (shape.CellExistsN(propName, VisExistsFlags.visExistsAnywhere)) return;

        var split = propName.Split('.');
        var prefix = split[0];
        var name = split[1];

        switch (prefix)
        {
            case "Prop":
                shape.AddNamedRow((short)VisSectionIndices.visSectionProp, name,
                    (short)tagVisRowTags.visTagDefault);
                break;
            case "User":
                shape.AddNamedRow((short)VisSectionIndices.visSectionUser, name,
                    (short)tagVisRowTags.visTagDefault);

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(propName), propName);
        }
    }

    /// <summary>
    ///     Set the target <see cref="IVShape" /> formual with double value.
    /// </summary>
    /// <param name="shape"></param>
    /// <param name="propName"></param>
    /// <param name="value"></param>
    /// <param name="createIfNotExists"></param>
    public static void TrySetValue(this IVShape shape, string propName, object value, bool createIfNotExists = false)
    {
        try
        {
            if (createIfNotExists)
                CreateIfNotExist(shape, propName);

            shape.CellsU[propName].SetValue(value);
        }
        catch (Exception e)
        {
            LogHost.Default.Error(e,
                $"Unable to set value for {propName} becasue it doesn't exist for shape {shape.ID}");
        }
    }


    private static void SetValue(this Cell source, object value)
    {
        if (source.FormulaU.StartsWith("GUARD"))
        {
            LogHost.Default.Warn($"Unable to set value for {source.Name} becasue it is guarded");
            return;
        }

        if (value is string str)
            source.FormulaU = $"\"{str}\"";
        else if (value is int i)
            source.FormulaU = i.ToString();
        else if (value is double d)
            source.FormulaU = d.ToString(CultureInfo.InvariantCulture);
        else if (value is decimal c)
            source.FormulaU = c.ToString(CultureInfo.InvariantCulture);
    }
}