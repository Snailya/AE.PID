using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Forms;
using AE.PID.Models;
using AE.PID.Properties;
using AE.PID.ViewModels;
using DynamicData;
using Microsoft.Office.Interop.Visio;
using MiniExcelLibs;
using ReactiveUI;
using Splat;

namespace AE.PID.Services;

/// <summary>
///     Dealing with extracting data from shape sheet and exporting.
/// </summary>
public class ProjectService : PageServiceBase
{
    private readonly SourceCache<ElementBase, int> _elements = new(t => t.Id);

    #region Output Properties

    public IObservableCache<ElementBase, int> Elements => _elements.AsObservableCache();

    #endregion

    public override void Start()
    {
        if (CleanUp.Any()) return;

        // observe the shape added event
        Observable.FromEvent<EPage_ShapeAddedEventHandler, Shape>(
                handler => Globals.ThisAddIn.Application.ActivePage.ShapeAdded += handler,
                handler => Globals.ThisAddIn.Application.ActivePage.ShapeAdded -= handler)
            .Select(TransformToElement)
            .WhereNotNull()
            .Subscribe(element => { _elements.AddOrUpdate(element); })
            .DisposeWith(CleanUp);

        // when a shape is deleted from the page, it could be captured by BeforeShapeDelete event
        Observable.FromEvent<EPage_BeforeShapeDeleteEventHandler, Shape>(
                handler => Globals.ThisAddIn.Application.ActivePage.BeforeShapeDelete += handler,
                handler => Globals.ThisAddIn.Application.ActivePage.BeforeShapeDelete -= handler)
            .Where(ShapePredicate())
            .Subscribe(shape =>
            {
                var toRemove = _elements.Lookup(shape.ID);
                _elements.Remove(toRemove.Value);
                if (toRemove.Value is IDisposable disposable) disposable.Dispose();
            })
            .DisposeWith(CleanUp);
    }

    public void LoadElements()
    {
        var elements = Globals.ThisAddIn.Application.ActivePage.Shapes.OfType<Shape>()
            .Select(TransformToElement)
            .Where(x => x != null)
            .Select(x => x!).ToList();
        _elements.AddOrUpdate(elements);
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
            MiniExcel.SaveAsByTemplate(dialog.FileName, Resources.TEMPLATE_parts_list,
                new
                {
                    Parts = partItems,
                    documentInfo.CustomerName,
                    documentInfo.DocumentNo,
                    documentInfo.ProjectNo,
                    documentInfo.VersionNo
                });

            WindowManager.ShowDialog("执行成功", MessageBoxButton.OK);
        }
        catch (Exception ex)
        {
            this.Log().Error(ex, "Failed to export.");
            WindowManager.ShowDialog($"执行失败。{ex.Message}", MessageBoxButton.OK);
        }
    }

    private static ElementBase? TransformToElement(Shape shape)
    {
        if (IsFunctionalGroupPredicate().Invoke(shape))
            return new FunctionalGroup(shape);
        if (IsUnitPredicate().Invoke(shape))
            return new EquipmentUnit(shape);
        if (IsEquipmentPredicate().Invoke(shape))
            return new Equipment(shape);
        if (IsInstrumentPredicate().Invoke(shape))
            return new Instrument(shape);
        if (IsFunctionalElementPredicate().Invoke(shape))
            return new FunctionalElement(shape);
        return null;
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
        return _elements.Items.Where(x => x.ParentId == 0)
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

        foreach (var sourceFunctionalGroup in _elements.Items
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
    private IEnumerable<ElementBase> GetChildren(ElementBase parent)
    {
        var list = new List<ElementBase> { parent };

        foreach (var child in _elements.Items.Where(x => x.ParentId == parent.Id))
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