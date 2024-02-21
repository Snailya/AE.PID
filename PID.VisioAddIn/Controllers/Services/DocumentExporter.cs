using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Forms;
using AE.PID.Models.BOM;
using AE.PID.Models.Exceptions;
using AE.PID.Models.VisProps;
using AE.PID.Properties;
using AE.PID.Views;
using Microsoft.Office.Interop.Visio;
using MiniExcelLibs;
using NLog;

namespace AE.PID.Controllers.Services;

/// <summary>
///     Dealing with extracting data from shape sheet and export that data into different format in excel.
/// </summary>
public abstract class DocumentExporter
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static Subject<Unit> ManuallyInvokeTrigger { get; } = new();

    /// <summary>
    ///     Emit a value manually
    /// </summary>
    public static void Invoke()
    {
        ManuallyInvokeTrigger.OnNext(Unit.Default);
    }

    /// <summary>
    /// Start listening for export button click event and display a view to accept user operation.
    /// The view prompt user to input extra information for project and the subsequent is called in ViewModel. 
    /// </summary>
    /// <returns></returns>
    public static IDisposable Listen()
    {
        Logger.Info("Export Service started.");

        return ManuallyInvokeTrigger
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(Globals.ThisAddIn.SynchronizationContext)
            .Subscribe(
                x =>
                {
                    try
                    {
                        Globals.ThisAddIn.MainWindow.Content = new ExportView();
                        Globals.ThisAddIn.MainWindow
                            .Show(); // this observable only display the view, not focus on any task
                    }
                    catch (Exception ex)
                    {
                        ThisAddIn.Alert($"加载失败：{ex.Message}");
                        Logger.Error(ex,
                            "Failed to display export window.");
                    }
                },
                ex =>
                {
                    Logger.Error(ex,
                        "Export Service ternimated accidently.");
                },
                () => { Logger.Error("Export Service should never complete."); }
            );
    }

    /// <summary>
    /// Convert all shapes in BOM layers to line items.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<LineItemBase> GetLineItems()
    {
        var items = Globals.ThisAddIn.Application.ActivePage?
            .CreateSelection(VisSelectionTypes.visSelTypeByLayer, VisSelectMode.visSelModeSkipSuper,
                string.Join(";", Globals.ThisAddIn.Configuration.ExportSettings.BomLayers)).OfType<IVShape>()
            .Select(x => x.ToLineItem()).ToList();

        var itemTree = ConvertToTree(items);
        return itemTree;
        
        // filter top level items
        var topLevelItems = items.Where(x => x.ParentId == null)
            .OrderBy(x => x.FunctionalGroup).ThenBy(x => x.Name)
            .ToList();

        // filter children
        var childrenDic = items.Where(x => x.ParentId != null)
            .GroupBy(x => x.ParentId)
            .ToDictionary(x => x.Key, g => g.ToList());

        // append children to topLevelItems
        foreach (var item in topLevelItems)
            if (childrenDic.TryGetValue(item.Id, out var children))
            {
                item.Children = children;
                foreach (var child in item.Children)
                    child.FunctionalElement = item.FunctionalElement + "-" + child.FunctionalElement;
            }

        Logger.Info($"Found {topLevelItems.Count} bom items on current pages");

        return topLevelItems;
    }

    /// <summary>
    ///     extract data from shapes on layers defined in config and group them as BOM items.
    /// </summary>
    public static void SaveAsBom(IEnumerable<LineItemBase> baseItems, string customerName, string documentNo,
        string projectNo,
        string versionNo)
    {
        var configuration = Globals.ThisAddIn.Configuration;

        var dialog = new SaveFileDialog
        {
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Filter = @"Excel Files|*.xlsx|All Files|*.*""",
            Title = @"保存文件"
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            if (configuration.ExportSettings.BomLayers is null)
                throw new BOMLayersNullException();

            // convert raw items to BOM item and flatten the linked functional elements
            var bomLineItems = baseItems.SelectMany(ExportBOMLineItem.FromLineItem).ToList();

            // supply the in page properties
            var totalDic = bomLineItems
                .GroupBy(x => new { x.NameChinese, x.TechnicalDataChinese })
                .Select(group => new { group.Key, Value = group.Sum(x => x.Count) })
                .ToDictionary(x => x.Key, x => x.Value);
            var inGroupDic = bomLineItems
                .GroupBy(x => new { x.FunctionalGroup, x.NameChinese, x.TechnicalDataChinese })
                .Select(group => new { group.Key, Value = group.Sum(x => x.Count) })
                .ToDictionary(x => x.Key, x => x.Value);

            for (var index = 0; index < bomLineItems.Count; index++)
            {
                var lineItem = bomLineItems[index];
                lineItem.Index = index + 1;
                lineItem.Total = totalDic[new { lineItem.NameChinese, lineItem.TechnicalDataChinese }];
                lineItem.InGroup =
                    inGroupDic[new { lineItem.FunctionalGroup, lineItem.NameChinese, lineItem.TechnicalDataChinese }];
            }

            // write to xlsx
            MiniExcel.SaveAsByTemplate(dialog.FileName, Resources.BOM_template,
                new
                {
                    Parts = bomLineItems,
                    CustomerName = customerName,
                    DocumentNo = documentNo,
                    ProjectNo = projectNo,
                    VersionNo = versionNo
                });

            ThisAddIn.Alert($"执行成功");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to export.");
            ThisAddIn.Alert($"执行失败。{ex.Message}");
        }
    }
    
    private static List<LineItemBase> ConvertToTree(List<LineItemBase> flatList)
    {
        var itemDictionary = flatList.ToDictionary(item => item.Id);
        var tree = new List<LineItemBase>();

        foreach (var item in flatList)
        {
            if (item.ParentId == null)
            {
                tree.Add(item);
            }
            else
            {
                if (itemDictionary.TryGetValue(item.ParentId.Value, out var parent))
                {
                    parent.Children ??= [];
                    parent.Children.Add(item);

                    if (item.Type == LineItemType.AttachedEquipment)
                        item.FunctionalElement = parent.FunctionalElement + "-" + item.FunctionalElement;
                }
                else
                {
                    // Handle the case where parent is not found, if needed
                    // (e.g., log a warning, skip the item, etc.)
                }
            }
        }

        return tree;
    }
}