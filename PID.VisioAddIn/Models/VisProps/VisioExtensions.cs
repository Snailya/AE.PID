using System;
using System.Windows.Forms;
using AE.PID.Interfaces;
using AE.PID.Models;
using Microsoft.Office.Interop.Visio;
using PID.VisioAddIn.Properties;

namespace PID.VisioAddIn.Models.VisProps;

public static class VisioExtensions
{
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
}