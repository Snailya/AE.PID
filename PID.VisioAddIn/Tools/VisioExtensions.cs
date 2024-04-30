using System;
using System.ComponentModel;
using System.Configuration;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using AE.PID.Interfaces;
using AE.PID.Models;
using AE.PID.Properties;
using DynamicData.Binding;
using Microsoft.Office.Interop.Visio;
using NLog;
using ReactiveUI;

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


    #region Binding Mixins

    public static IDisposable OneWayBind<TModel, TMProperty>(this Shape shape, TModel model,
        Expression<Func<TModel, TMProperty>> mProperty, string visioPropertyName,
        Func<string, TMProperty?>? visioToModelConverterOverride = null)
    {
        var visioPropertyToPropertyConverter =
            visioToModelConverterOverride ?? (value => (TMProperty)Convert.ChangeType(value, typeof(TMProperty)));

        var vmExpression = Reflection.Rewrite(mProperty.Body);

        return (shape.CellExists[visioPropertyName, (short)VisExistsFlags.visExistsAnywhere] == (short)VbaBool.True
                ? Observable.Return(shape.Cells[visioPropertyName])
                : Observable.Empty<Cell>())
            .Merge(Observable.FromEvent<EShape_CellChangedEventHandler, Cell>(
                    handler => shape.CellChanged += handler,
                    handler => shape.CellChanged -= handler)
                .Where(x => x.Name == visioPropertyName))
            .Select(x => x.TryGetFormatValue() ?? string.Empty)
            .Select(visioPropertyToPropertyConverter)
            .Subscribe(value =>
            {
                Reflection.TrySetValueToPropertyChain(model, vmExpression.GetExpressionChain(), value!);
            });
    }

    public static IDisposable Bind<TModel, TMProperty>(this Shape shape, TModel model,
        Expression<Func<TModel, TMProperty>> mProperty, string visioPropertyName,
        Func<string, TMProperty?>? visioToModelConverterOverride = null,
        Func<TMProperty, string?>? modelToVisioConverterOverride = null) where TModel : INotifyPropertyChanged
    {
        var visioToModelConverter =
            visioToModelConverterOverride ?? (value => (TMProperty)Convert.ChangeType(value, typeof(TMProperty)));
        var modelToVisioConverter =
            modelToVisioConverterOverride ?? (value => (string)Convert.ChangeType(value, typeof(string)));

        var vmExpression = Reflection.Rewrite(mProperty.Body);

        var d = new CompositeDisposable();

        (shape.CellExists[visioPropertyName, (short)VisExistsFlags.visExistsAnywhere] == (short)VbaBool.True
                ? Observable.Return(shape.Cells[visioPropertyName])
                : Observable.Empty<Cell>())
            .Merge(Observable.FromEvent<EShape_CellChangedEventHandler, Cell>(
                    handler => shape.CellChanged += handler,
                    handler => shape.CellChanged -= handler)
                .Where(x => x.Name == visioPropertyName))
            .Select(x => x.TryGetFormatValue() ?? string.Empty)
            .Select(visioToModelConverter)
            .DistinctUntilChanged()
            .Subscribe(value =>
            {
                Reflection.TrySetValueToPropertyChain(model, vmExpression.GetExpressionChain(), value!);
            })
            .DisposeWith(d);

        // observe the model property change to synchronize from model to visio
        model.WhenValueChanged(mProperty)
            .DistinctUntilChanged()
            .WhereNotNull()
            .Select(modelToVisioConverter)
            .Where(_ => shape.CellExists[visioPropertyName, (short)VisExistsFlags.visExistsAnywhere] ==
                        (short)VbaBool.True)
            .Select(x => x?.ClearFormat(shape, visioPropertyName) ?? string.Empty)
            .Subscribe(value => { shape.Cells[visioPropertyName].UpdateIfChanged(value); })
            .DisposeWith(d);

        return d;
    }

    #endregion

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
    private static string? TryGetFormatValue(this IVCell cell)
    {
        var row = cell.ContainingRow;
        return row.GetFormatString();
    }

    /// <summary>
    ///     Clear the format return the origin string.
    /// </summary>
    /// <param name="formatValue"></param>
    /// <param name="shape"></param>
    /// <param name="propName"></param>
    /// <returns></returns>
    private static string ClearFormat(this string formatValue, IVShape shape, string propName)
    {
        var format = shape.Cells[propName].ContainingRow.CellU[VisCellIndices.visCustPropsFormat]
            .ResultStr[VisUnitCodes.visUnitsString];
        if (string.IsNullOrEmpty(format)) return formatValue;

        var pattern = Regex.Replace(format, @"(\\.)|(@)|(0\.[#0]+)|(#\\)", match =>
        {
            if (match.Groups[1].Success)
                return match.Groups[1].Value.Substring(1); // Replace \\char with char

            if (match.Groups[2].Success)
                return @"(\w+)"; // Replace @ with the \w+

            if (match.Groups[3].Success)
                return @"(\d+\.\d+)"; // Handle other numeric patterns

            if (match.Groups[4].Success)
                return @"(\d+)";

            return match.Value;
        });

        var result = Regex.Match(formatValue, pattern).Groups[1].Value;
        return result;
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
            if (MessageBox.Show(string.Format(Resources.Property_Value_Override_Confirmation, cell.Name,
                        cell.Shape.Name,
                        oldFormula, newValue, Environment.NewLine + Environment.NewLine), @"属性值覆盖确认",
                    MessageBoxButtons.YesNo) == DialogResult.No)
                return false;
        cell.FormulaU = newFormula;

        return true;
    }

    #endregion
}