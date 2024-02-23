using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Forms;
using AE.PID.Models.BOM;
using AE.PID.Models.Exceptions;
using AE.PID.Models.VisProps;
using AE.PID.Properties;
using AE.PID.ViewModels;
using AE.PID.Views.BOM;
using DynamicData;
using Microsoft.Office.Interop.Visio;
using MiniExcelLibs;
using NLog;

namespace AE.PID.Controllers.Services;

/// <summary>
///     Dealing with extracting data from shape sheet and exporting.
/// </summary>
public class DocumentExporter : IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static Subject<Unit> ManuallyInvokeTrigger { get; } = new();

    private readonly IDisposable _cleanUp;
    private readonly IList<string> _validLayers;
    private readonly SourceCache<Element, int> _elements = new(t => t.Id);

    public DocumentExporter(Page page)
    {
        _validLayers = Globals.ThisAddIn.Configuration.ExportSettings.BomLayers;

        // initialize items by get all items from current page
        _elements.AddOrUpdate(GetElementsFromPage(page));

        var listenOnShapeChange = ListenOnShapeChange(page);

        _cleanUp = Disposable.Create(() =>
        {
            listenOnShapeChange.Dispose();
            _elements.Dispose();
        });
    }
    
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

    // We expose the Connect() since we are interested in a stream of changes.
    // If we have more than one subscriber, and the subscribers are known, 
    // it is recommended you look into the Reactive Extension method Publish().
    public IObservable<IChangeSet<Element, int>> Connect()
    {
        return _elements.Connect();
    }
    
    /// <summary>
    ///     extract data from shapes on layers defined in config and group them as BOM items.
    /// </summary>
    public void ExportToExcel(DocumentInfoViewModel documentInfo)
    {
        var dialog = new SaveFileDialog
        {
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Filter = @"Excel Files|*.xlsx|All Files|*.*""",
            Title = @"保存文件"
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            if (_validLayers.Count == 0) throw new BOMLayersNullException();

            // sort the top level elements
            var orderedElements = _elements.Items.Where(x => x.ParentId == 0).ToList();
            orderedElements.Sort();

            // append nested elements to it's parent
            // in order to avoid the parent is not in ordered list, order the elements by type so that the attached element will be treated at last
            foreach (var item in _elements.Items.Where(x => x.ParentId != 0).OrderBy(x => x.Type).ThenBy(x => x.Name))
            {
                var parent = orderedElements.Single(x => x.Id == item.ParentId);
                var parentIndex = orderedElements.IndexOf(parent);
                orderedElements.Insert(parentIndex + 1, item);
                
                // overwrite the function element property if it is a Attached element
                if (item.Type == ElementType.Attached)
                    item.FunctionalElement = $"{parent.FunctionalElement}-{item.FunctionalElement}";
            }

            var materials = orderedElements.Select(Material.FromElement).ToList();

            // append aggregated properties
            var totalDic = materials
                .GroupBy(x => new { x.NameChinese, x.TechnicalDataChinese })
                .Select(group => new { group.Key, Value = group.Sum(x => x.Count) })
                .ToDictionary(x => x.Key, x => x.Value);
            var inGroupDic = materials
                .GroupBy(x => new { x.FunctionalGroup, x.NameChinese, x.TechnicalDataChinese })
                .Select(group => new { group.Key, Value = group.Sum(x => x.Count) })
                .ToDictionary(x => x.Key, x => x.Value);

            for (var index = 0; index < materials.Count; index++)
            {
                var material = materials[index];
                material.Index = index + 1;
                material.Total = totalDic[new { material.NameChinese, material.TechnicalDataChinese }];
                material.InGroup =
                    inGroupDic[new { material.FunctionalGroup, material.NameChinese, material.TechnicalDataChinese }];
            }

            // write to xlsx
            MiniExcel.SaveAsByTemplate(dialog.FileName, Resources.BOM_template,
                new
                {
                    Parts = materials,
                    CustomerName = documentInfo.CustomerName,
                    DocumentNo = documentInfo.DocumentNo,
                    ProjectNo = documentInfo.ProjectNo,
                    VersionNo = documentInfo.VersionNo
                });

            ThisAddIn.Alert($"执行成功");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to export.");
            ThisAddIn.Alert($"执行失败。{ex.Message}");
        }
    }
    
    public void Dispose()
    {
        _cleanUp.Dispose();
    }

    /// <summary>
    /// Listen on the shape modification, addition and delete from the page to make element dynamic.
    /// </summary>
    /// <param name="page"></param>
    /// <returns>A <see cref="Disposable"/> to unsubscribe from these change events</returns>
    private IDisposable ListenOnShapeChange(EPage_Event page)
    {
        // when a shape's property is modified, it will raise up FormulaChanged event, so that the modification could be captured to emit as a new value
        var modifySubscription = Observable
            .FromEvent<EPage_FormulaChangedEventHandler, Cell>(
                handler => page.FormulaChanged += handler,
                handler => page.FormulaChanged -= handler)
            .Where(cell => cell.Shape.IsOnLayers(_validLayers))
            .Subscribe(cell =>
            {
                var item = cell.Shape.ToElement();
                if (item != null) _elements.AddOrUpdate(item);
            });
        
        // when a new shape is add to the page, it could be captured using ShapeAdded event
        var addSubscription = Observable.FromEvent<EPage_ShapeAddedEventHandler, Shape>(
                handler => page.ShapeAdded += handler,
                handler => page.ShapeAdded -= handler)
            .Where(shape => shape.IsOnLayers(_validLayers))
            .Subscribe(shape =>
            {
                var item = shape.ToElement();
                if (item != null) _elements.AddOrUpdate(item);
            });
        
        // when a shape is deleted from the page, it could be captured by BeforeShapeDelete event
        var deleteSubscription = Observable.FromEvent<EPage_BeforeShapeDeleteEventHandler, Shape>(
                handler => page.BeforeShapeDelete += handler,
                handler => page.BeforeShapeDelete -= handler)
            .Where(shape => shape.IsOnLayers(_validLayers))
            .Subscribe(shape => { _elements.RemoveKey(shape.ID); });

        // return a disposable to unsubscribe from all these change event
        return Disposable.Create(() =>
        {
            modifySubscription.Dispose();
            addSubscription.Dispose();
            deleteSubscription.Dispose();
        });
    }

    /// <summary>
    /// Convert all shapes in BOM layers to elements. elements reflects only the raw data on the page. 
    /// </summary>
    /// <returns>A collection of elements from page</returns>
    private IEnumerable<Element> GetElementsFromPage(IVPage page)
    {
        var elements = page.CreateSelection(
                VisSelectionTypes.visSelTypeByLayer, VisSelectMode.visSelModeSkipSuper, string.Join(";", _validLayers))
            .OfType<IVShape>()
            .Select(x => x.ToElement())
            .Where(x => x is not null)
            .OfType<Element>().ToList();

        return elements;
    }
}