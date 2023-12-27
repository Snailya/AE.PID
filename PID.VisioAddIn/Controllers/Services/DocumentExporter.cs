using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using AE.PID.Models;
using AE.PID.Models.Exceptions;
using AE.PID.Properties;
using AE.PID.Views;
using Microsoft.Office.Interop.Visio;
using MiniExcelLibs;
using NLog;
using PID.VisioAddIn.Properties;

namespace AE.PID.Controllers.Services;

/// <summary>
///     Dealing with extracting data from shape sheet and export that data into different format in excel.
/// </summary>
public abstract class DocumentExporter
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static Subject<IVPage> ManuallyInvokeTrigger { get; } = new();

    /// <summary>
    ///     Emit a value manually
    /// </summary>
    /// <param name="page"></param>
    public static void Invoke(IVPage page)
    {
        ManuallyInvokeTrigger.OnNext(page);
    }

    /// <summary>
    /// Start listening for export button click event and display a view to accept user operation.
    /// The view prompt user to input extra information for project and the subsequent is called in ViewModel. 
    /// </summary>
    /// <returns></returns>
    public static IDisposable Listen()
    {
        Logger.Info($"Export Service started.");

        return ManuallyInvokeTrigger
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(Globals.ThisAddIn.SynchronizationContext)
            .Select(_ =>
            {
                Globals.ThisAddIn.MainWindow.Content = new ExportView();
                Globals.ThisAddIn.MainWindow.Show(); // this observable only display the view, not focus on any task

                return Unit.Default;
            })
            .Subscribe(
                _ => { },
                ex =>
                {
                    ThisAddIn.Alert(ex.Message);
                    Logger.Error(ex,
                        $"Export Service ternimated accidently.");
                },
                () => { Logger.Error("Export Service should never complete."); }
            );
    }

    /// <summary>
    ///     extract data from shapes on layers defined in config and group them as BOM items.
    /// </summary>
    public static void SaveAsBom(IVPage page, string customerName, string documentNo, string projectNo,
        string versionNo)
    {
        var configuration = Globals.ThisAddIn.Configuration;

        var dialog = new SaveFileDialog
        {
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Filter = "Excel Files|*.xlsx|All Files|*.*\"",
            Title = "保存文件"
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            if (configuration.ExportSettings.BomLayers is null)
                throw new BOMLayersNullException();

            var selection = page
                .CreateSelection(VisSelectionTypes.visSelTypeByLayer, VisSelectMode.visSelModeSkipSuper,
                    string.Join(";", configuration.ExportSettings.BomLayers));

            var partItems = new List<PartItem>();
            foreach (IVShape shape in selection)
            {
                var item = new PartItem
                {
                    ProcessZone = shape.Cells["Prop.ProcessZone"].ResultStr[VisUnitCodes.visUnitsString],
                    FunctionalGroup = shape.Cells["Prop.FunctionalGroup"].ResultStr[VisUnitCodes.visUnitsString],
                    FunctionalElement = TryGetFormatValue(shape, "Prop.FunctionalElement"),
                    Name = shape.Cells["Prop.SubClass"].ResultStr[VisUnitCodes.visUnitsString],
                    TechnicalData = GetTechnicalData(shape)
                };

                if (double.TryParse(shape.Cells["Prop.Subtotal"].ResultStr[VisUnitCodes.visUnitsString], out var count))
                    item.Count = count;

                partItems.Add(item);
            }

            var totalDic = partItems
                .GroupBy(x => new { x.ProcessZone, x.Name, x.TechnicalData })
                .Select(group => new { group.Key, Value = group.Sum(x => x.Count) })
                .ToDictionary(x => x.Key, x => x.Value);
            var inGroupDic = partItems
                .GroupBy(x => new { x.FunctionalGroup, x.Name, x.TechnicalData })
                .Select(group => new { group.Key, Value = group.Sum(x => x.Count) })
                .ToDictionary(x => x.Key, x => x.Value);

            var extendPartItems = partItems.Select(x => new
            {
                processarea = x.ProcessZone,
                functionalgroup = x.FunctionalGroup,
                functionalelement = x.FunctionalElement,
                name = x.Name,
                technicaldata = x.TechnicalData,
                total = totalDic[new { x.ProcessZone, x.Name, x.TechnicalData }],
                ingroup = inGroupDic[new { x.FunctionalGroup, x.Name, x.TechnicalData }]
            });

            // write to xlsx
            MiniExcel.SaveAsByTemplate(dialog.FileName, Resources.BOM_template,
                new
                {
                    parts = extendPartItems,
                    customer = customerName, document = documentNo, project = projectNo, version = versionNo
                });

            ThisAddIn.Alert($"执行成功");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to export.");
            ThisAddIn.Alert($"执行失败。{ex.Message}");
        }
    }

    private static string GetTechnicalData(IVShape shape)
    {
        StringBuilder stringBuilder = new();

        for (var i = 0; i < shape.RowCount[(short)VisSectionIndices.visSectionProp]; i++)
        {
            // skip common properties
            var sort = shape
                .CellsSRC[(short)VisSectionIndices.visSectionProp, (short)i, (short)VisCellIndices.visCustPropsSortKey]
                .ResultStr[VisUnitCodes.visUnitsString];
            if (!string.IsNullOrEmpty(sort)) continue;

            // skip empty value
            var value = GetFormatValue(shape.CellsSRC[(short)VisSectionIndices.visSectionProp, (short)i,
                (short)VisCellIndices.visCustPropsSortKey].ContainingRow);
            if (string.IsNullOrEmpty(value)) continue;

            var label = shape
                .CellsSRC[(short)VisSectionIndices.visSectionProp, (short)i, (short)VisCellIndices.visCustPropsLabel]
                .ResultStr[VisUnitCodes.visUnitsString];

            stringBuilder.Append($"{label}: {value}; ");
        }

        return stringBuilder.ToString();
    }

    private static string TryGetFormatValue(IVShape shape, string propName)
    {
        var existsAnywhere = shape.CellExists[propName, (short)VisExistsFlags.visExistsAnywhere] ==
                             (short)VBABool.True;
        if (!existsAnywhere) return null;
        var row = shape.Cells[propName].ContainingRow;

        return GetFormatValue(row);
    }

    private static string GetFormatValue(IVRow row)
    {
        try
        {
            var value = row.CellU[VisCellIndices.visCustPropsValue].ResultStr[VisUnitCodes.visUnitsString];
            if (string.IsNullOrEmpty(value)) return value;

            var type = row.CellU[VisCellIndices.visCustPropsType].ResultStr[VisUnitCodes.visNoCast];

            if (type != "0" && type != "2") return value;
            var format = row.CellU[VisCellIndices.visCustPropsFormat].ResultStr[VisUnitCodes.visUnitsString];
            if (!string.IsNullOrEmpty(format))
                value = Globals.ThisAddIn.Application.FormatResult(value, "", "", format);

            return value;
        }
        catch (COMException comException)
        {
            Logger.Error(comException, $"Failed to get value form {row.Shape.ID}!{row.Name}.");
            throw new FormatValueInvalidException(row.Shape.ID, row.Name);
        }
    }
}