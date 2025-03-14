using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using AE.PID.Client.Core.VisioExt.Control;
using AE.PID.Client.VisioAddIn.Properties;
using ClosedXML.Excel;
using Microsoft.Office.Interop.Visio;
using Splat;
using Path = System.IO.Path;

namespace AE.PID.Client.VisioAddIn;

public static class ElectricalControlSpecificationHelper
{
    public static void Generate(IVDocument document, string? filePath = null)
    {
        try
        {
            filePath ??= Path.ChangeExtension(document.FullName, ".xlsx");

            var groupedItems = document.Pages.OfType<Page>()
                .SelectMany(x => x.Shapes.OfType<Shape>())
                .Where(x => x.Master != null)
                .Select(x => x.CreateFromShape())
                .Where(x => x != null)
                .Cast<ElectricalControlSpecificationItemBase>()
                .OrderBy(x => x.FullDesignation)
                .GroupBy(x => x.Type)
                .ToList();

            LogHost.Default.Info($"Found {groupedItems.Sum(x => x.Count())} electrical control specification items.");

            // write to excel
            using (var memoryStream = new MemoryStream(Resources.TEMPLATE_Electrical_Control_Specification))
            {
                memoryStream.Position = 0; // 重置位置指针

                using (var workbook = new XLWorkbook(memoryStream))
                {
                    foreach (var grouping in groupedItems)
                    {
                        var type = grouping.Key;

                        if (type == typeof(Instrument) )
                        {
                            // search the sheet to find the index
                            var attr = type.GetCustomAttribute<ElectricalControlSpecificationItem>();
                            if (attr is { SheetName: not null })
                            {
                                // if no worksheet found
                                if (!workbook.Worksheets.TryGetWorksheet(attr.SheetName, out var worksheet))
                                {
                                    LogHost.Default.Warn($"没有找到工作表：{attr.SheetName}");
                                    continue;
                                }

                                var groupedInstruments = grouping.Cast<Instrument>()
                                    .GroupBy(x => x.Designation.Substring(0, 2))
                                    .ToList();

                                foreach (var groupingInstruments in groupedInstruments)
                                    if (TryGetTargetCellByProcessVariable(groupingInstruments.Key, worksheet,
                                            out var targetCell))
                                    {
                                        var data = groupingInstruments
                                            .SelectMany(x => x.Flatten())
                                            .OrderBy(x => x.FullDesignation)
                                            .Select((x, i) => x.ToDataRow(i + 1))
                                            .ToList();

                                        WriteData(targetCell, worksheet, data);
                                    }
                            }
                        }
                        else
                        {
                            if (TryGetTargetCellByType(type, workbook, out var worksheet, out var targetCell))
                            {
                                var data = grouping.SelectMany(x => x.Flatten())
                                    .OrderBy(x => x.FullDesignation)
                                    .Select((x, i) => x.ToDataRow(i + 1))
                                    .ToList();

                                WriteData(targetCell, worksheet, data);
                            }
                        }
                    }

                    workbook.SaveAs(filePath);
                }
            }


            // 打开文件位置
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            // log
            LogHost.Default.Error(ex, "Failed to export electrical control specification items");

            // display error message
            MessageBox.Show(ex.Message, "生成电控任务书失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void WriteData(IXLCell? cell, IXLWorksheet? worksheet, List<object[]> data)
    {
        // if there exist the section, write down the data
        var newRow = cell!.Address.RowNumber + 2; // 行号向下偏移 2
        var column = cell.Address.ColumnNumber; // 列号不变

        var newCell = worksheet!.Cell(newRow, column);

        // insert row
        newCell.WorksheetRow().InsertRowsBelow(data.Count);

        // insert data
        newCell.InsertData(data);
    }

    private static bool TryGetTargetCellByType(Type type, XLWorkbook workbook, out IXLWorksheet? worksheet,
        out IXLCell? cell)
    {
        worksheet = null;
        cell = null;

        // search the sheet to find the index
        var attr = type.GetCustomAttribute<ElectricalControlSpecificationItem>();
        var searchText = attr.SectionName;

        // if no worksheet found
        if (attr.SheetName == null || !workbook.Worksheets.TryGetWorksheet(attr.SheetName, out worksheet))
        {
            LogHost.Default.Warn($"没有找到工作表：{attr.SheetName}");
            return false;
        }

        cell = worksheet.Column("A").CellsUsed(c => string.IsNullOrEmpty(c.FormulaA1) && c.GetString() == searchText)
            .SingleOrDefault();

        return cell != null;
    }

    private static bool TryGetTargetCellByProcessVariable(string processVariable, IXLWorksheet worksheet,
        out IXLCell? cell)
    {
        cell = null;

        cell = worksheet.Column("A")
            .CellsUsed(c => string.IsNullOrEmpty(c.FormulaA1) && c.GetString().Contains(processVariable))
            .FirstOrDefault();

        return cell != null;
    }
}