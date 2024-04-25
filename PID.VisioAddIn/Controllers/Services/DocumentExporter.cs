using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Forms;
using AE.PID.Models.BOM;
using AE.PID.Properties;
using AE.PID.ViewModels.Components;
using DynamicData.Binding;
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
    private readonly CompositeDisposable _cleanup = new();
    private readonly Page _page;

    public readonly ObservableCollectionExtended<Element> Elements = [];

    public DocumentExporter(Page page)
    {
        Contract.Assert(page != null,
            "Could not initialize exporter on null page.");

        _page = page!;

        // observe the shape added event
        Observable.FromEvent<EPage_ShapeAddedEventHandler, Shape>(
                handler => _page.ShapeAdded += handler,
                handler => _page.ShapeAdded -= handler)
            // switch to background thread
            .ObserveOn(ThreadPoolScheduler.Instance)
            .Where(ShapePredicate())
            .Subscribe(shape =>
            {
                if (IsFunctionalGroupPredicate().Invoke(shape))
                    Elements.Add(new FunctionalGroup(shape));
                else if (IsUnitPredicate().Invoke(shape))
                    Elements.Add(new EquipmentUnit(shape));
                else if (IsEquipmentPredicate().Invoke(shape))
                    Elements.Add(new Equipment(shape));
                else if (IsFunctionalElementPredicate().Invoke(shape))
                    Elements.Add(new FunctionalElement(shape));
            })
            .DisposeWith(_cleanup);

        // when a shape is deleted from the page, it could be captured by BeforeShapeDelete event
        Observable.FromEvent<EPage_BeforeShapeDeleteEventHandler, Shape>(
                handler => _page.BeforeShapeDelete += handler,
                handler => _page.BeforeShapeDelete -= handler)
            // switch to background thread
            .ObserveOn(ThreadPoolScheduler.Instance)
            .Where(ShapePredicate())
            .Subscribe(shape =>
            {
                var toRemove = Elements.Single(x => x.Id == shape.ID);
                Elements.Remove(toRemove);

                if (toRemove is IDisposable disposable) disposable.Dispose();
            })
            .DisposeWith(_cleanup);

        // todo: maybe load in background?
        // initialize
        Elements.AddRange(_page.Shapes.OfType<Shape>()
            .Where(IsFunctionalGroupPredicate())
            .Select(x => new FunctionalGroup(x)));
        Elements.AddRange(_page.Shapes.OfType<Shape>()
            .Where(IsUnitPredicate())
            .Select(x => new EquipmentUnit(x)));
        Elements.AddRange(_page.Shapes.OfType<Shape>()
            .Where(IsEquipmentPredicate())
            .Select(x => new Equipment(x)));
        Elements.AddRange(_page.Shapes.OfType<Shape>()
            .Where(IsInstrumentPredicate())
            .Select(x => new Instrument(x)));
        Elements.AddRange(_page.Shapes.OfType<Shape>()
            .Where(IsFunctionalElementPredicate())
            .Select(x => new FunctionalElement(x)));
    }

    public void Dispose()
    {
        _cleanup.Dispose();
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
            var partItems = PopulatePartListTableLineItems();
            MiniExcel.SaveAsByTemplate(dialog.FileName, Resources.BOM_template,
                new
                {
                    Parts = partItems,
                    documentInfo.CustomerName,
                    documentInfo.DocumentNo,
                    documentInfo.ProjectNo,
                    documentInfo.VersionNo
                });

            ThisAddIn.Alert("执行成功");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to export.");
            ThisAddIn.Alert($"执行失败。{ex.Message}");
        }
    }

    #region Part List Table

    /// <summary>
    ///     Populate line items for part list table
    /// </summary>
    /// <returns></returns>
    private List<PartListTableLineItem> PopulatePartListTableLineItems()
    {
        var realPartListItems = PopulateRealPartItems().ToList();
        var virtualPartListItems = PopulateVirtualPartListItems(realPartListItems);
        var partListItems = realPartListItems.Concat(virtualPartListItems).ToList();

        var grouped = partListItems
            .GroupBy(m => new
            {
                MaterialNo = string.IsNullOrEmpty(m.AEMaterialNo) ? Guid.NewGuid().ToString() : m.AEMaterialNo,
                m.FunctionalGroup
            })
            .Select(group => new
            {
                group.Key.MaterialNo,
                group.Key.FunctionalGroup,
                CountInGroup = group.Sum(m => m.Count),
                Total = partListItems.Where(m => m.AEMaterialNo == group.Key.MaterialNo).Sum(m => m.Count)
            });

        foreach (var group in grouped)
        foreach (var material in partListItems.Where(m =>
                     m.AEMaterialNo == group.MaterialNo && m.FunctionalGroup == group.FunctionalGroup))
        {
            material.InGroup = group.CountInGroup;
            material.Total = group.Total;
        }

        return partListItems;
    }

    /// <summary>
    ///     Flatten the elements in page and filter out only equipments and functional elements, then convert to exportable
    ///     items.
    /// </summary>
    /// <returns></returns>
    private IEnumerable<PartListTableLineItem> PopulateRealPartItems()
    {
        return Elements.Where(x => x.ParentId == 0)
            .OrderBy(x => x.Label)
            .SelectMany(GetChildren)
            .Where(x => x is PartItem).Cast<PartItem>()
            .Select(PartListTableLineItem.FromPartItem);
    }

    /// <summary>
    ///     Create virtual copies of the items for proxy functional group
    /// </summary>
    /// <param name="realPartListItems"></param>
    /// <returns></returns>
    private IEnumerable<PartListTableLineItem> PopulateVirtualPartListItems(
        IEnumerable<PartListTableLineItem> realPartListItems)
    {
        var virtualPartListItems = new List<PartListTableLineItem>();

        var partListItemsGroupedByFunctionalGroup = realPartListItems.GroupBy(x => x.FunctionalGroup).ToList();
        foreach (var sourceFunctionalGroup in Elements
                     .Where(x => x is FunctionalGroup functionalGroup && functionalGroup.Related.Any())
                     .Cast<FunctionalGroup>())
        foreach (var targetFunctionalGroup in sourceFunctionalGroup.Related)
        {
            var group = partListItemsGroupedByFunctionalGroup
                .SingleOrDefault(x => x.Key == sourceFunctionalGroup.Designation)?.ToList();
            if (group == null) continue;
            var virtualPartListItemsInGroup =
                group.Select(x => PartListTableLineItem.CopyTo(x, targetFunctionalGroup.Designation));
            virtualPartListItems.AddRange(virtualPartListItemsInGroup);
        }

        return virtualPartListItems;
    }

    /// <summary>
    ///     Hierarchically find out the elements.
    /// </summary>
    /// <param name="parent"></param>
    /// <returns></returns>
    private IEnumerable<Element> GetChildren(Element parent)
    {
        var list = new List<Element> { parent };
        foreach (var child in Elements.Where(x => x.ParentId == parent.Id))
        {
            var children = GetChildren(child);
            list.AddRange(children.OrderBy(x => x.Label));
        }

        return list;
    }

    #endregion


    #region Predicates

    private static Func<Shape, bool> IsFunctionalElementPredicate()
    {
        return x => x.HasCategory("FunctionalElement");
    }

    private static Func<Shape, bool> IsInstrumentPredicate()
    {
        return x => x.HasCategory("Instrument");
    }

    private static Func<Shape, bool> IsEquipmentPredicate()
    {
        return x => x.HasCategory("Equipment");
    }

    private static Func<Shape, bool> IsUnitPredicate()
    {
        return x => x.HasCategory("Unit");
    }

    private static Func<Shape, bool> IsFunctionalGroupPredicate()
    {
        return x => x.HasCategory("FunctionalGroup") && !x.HasCategory("Proxy");
    }

    private static Func<Shape, bool> ShapePredicate()
    {
        return x =>
            x.HasCategory("FunctionalElement") || x.HasCategory("Equipment") || x.HasCategory("Instrument") ||
            x.HasCategory("Unit") || (x.HasCategory("FunctionalGroup") && !x.HasCategory("Proxy"));
    }

    #endregion
}