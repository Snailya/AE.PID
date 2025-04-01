using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using AE.PID.Client.Core;
using AE.PID.Client.VisioAddIn.Properties;
using ClosedXML.Excel;
using Splat;

namespace AE.PID.Client.VisioAddIn;

public class ExportService : IExportService, IEnableLogger
{
    /// <inheritdoc />
    public void SaveAsJson<T>(string fileName, T data)
    {
        // serialize the data into json string
        var str = JsonSerializer.Serialize(data);

        // create the folder if the file is not an existed file and the directory also not exists
        if (!File.Exists(fileName) && Path.GetDirectoryName(fileName) is { } directoryName &&
            !Directory.Exists(directoryName)) Directory.CreateDirectory(directoryName);

        using var configFileStream = File.Open(fileName, FileMode.Create);
        using var configStreamWriter = new StreamWriter(configFileStream, Encoding.UTF8);
        configStreamWriter.Write(str);
        configStreamWriter.Flush();
    }

    public void ExportAsPartLists(PartListItem[] parts, string filePath)
    {
        try
        {
            LogHost.Default.Info($"Found {parts.Length} partlist items.");

            var groupedParts = parts.GroupBy(x => x.ProcessArea).ToArray();

            // update index
            foreach (var grouping in groupedParts)
            {
                // append index
                var i = 1;
                foreach (var part in grouping)
                {
                    part.Index = i;
                    i++;
                }
            }

            // write to excel
            using (var memoryStream = new MemoryStream(Resources.TEMPLATE_Parts_List))
            {
                memoryStream.Position = 0; // 重置位置指针

                using (var workbook = new XLWorkbook(memoryStream))
                {
                    if (groupedParts.Length == 1)
                    {
                        // insert data
                        var data = parts.Select(x => x.ToDataRow());
                        workbook.Worksheet(1).Cell(7, 1).InsertData(data);
                    }
                    else
                    {
                        foreach (var grouping in groupedParts)
                        {
                            var worksheetName = $"零件清单 Part List - {grouping.Key}";

                            // copy workbook
                            var worksheet = workbook.Worksheet(1).CopyTo(worksheetName);

                            // insert data
                            var data = grouping.Select(x => x.ToDataRow());
                            worksheet.Cell(7, 1).InsertData(data);
                        }

                        // remove the original one
                        if (workbook.Worksheets.Count > 1)
                            workbook.Worksheet(1).Delete();
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
            LogHost.Default.Error(ex, "Failed to export part list.");

            // display error message
            MessageBox.Show(ex.Message, "生成设备清单失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}