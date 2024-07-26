using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using AE.PID.Models;
using AE.PID.Properties;
using AE.PID.ViewModels;
using DynamicData;
using Microsoft.Office.Interop.Excel;
using Microsoft.Office.Interop.Visio;
using MiniExcelLibs;
using ReactiveUI;
using Splat;
using Shape = Microsoft.Office.Interop.Visio.Shape;

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

    public void ExportToPage()
    {
        // todo
        var oleShape = Globals.ThisAddIn.Application.ActivePage.InsertObject("Excel.Sheet",
            (short)VisInsertObjArgs.visInsertAsEmbed);
        object oleObject = oleShape.Object;
        var workbook = (Workbook)oleObject;

        // 操作Excel对象
        Worksheet worksheet = workbook.Worksheets[1];
        var dataArray = ToDataArray(PopulatePartListTableLineItems());
        worksheet.Range["A1"].Resize[dataArray.GetLength(0), dataArray.GetLength(1)].Value = dataArray;
        worksheet.Columns.AutoFit();

        // 保存并关闭Excel工作簿
        workbook.Save();
        workbook.Close(false); // 关闭工作簿，但不保存改变
        Marshal.ReleaseComObject(worksheet);
        Marshal.ReleaseComObject(workbook);

        return;

        string[,] ToDataArray(List<PartListTableLineItem> list)
        {
            var array = new string[list.Count + 2, 7];

            // append column name
            array[0, 0] = "序号";
            array[0, 1] = "功能元件";
            array[0, 2] = "描述";
            array[0, 3] = "供应商";
            array[0, 4] = "型号";
            array[0, 5] = "规格";
            array[0, 6] = "物料号";

            array[1, 0] = "Index";
            array[1, 1] = "Function Element";
            array[1, 2] = "Description";
            array[1, 3] = "Manufacturer";
            array[1, 4] = "Type";
            array[1, 5] = "Specification";
            array[1, 6] = "Material No.";

            // append data
            for (var i = 0; i < list.Count; i++)
            {
                var line = list[i];
                array[i + 2, 0] = (i + 1).ToString();
                array[i + 2, 1] = line.FunctionalElement;
                array[i + 2, 2] = line.Description;
                array[i + 2, 3] = line.Supplier;
                array[i + 2, 4] = line.Type;
                array[i + 2, 5] = line.Specification;
                array[i + 2, 6] = line.MaterialNo;
            }

            return array;
        }
    }

    /// <summary>
    ///     extract data from shapes on layers defined in config and group them as BOM items.
    /// </summary>
    public void ExportToExcel(string fileName, DocumentInfoViewModel documentInfo)
    {
        try
        {
            var partItems = PopulatePartListTableLineItems();
            MiniExcel.SaveAsByTemplate(fileName, Resources.TEMPLATE_parts_list,
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

    private class PartListTableLineItem
    {
        /// <summary>
        ///     Create a <see cref="PartListTableLineItem" /> from <see cref="PartItem" />.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static PartListTableLineItem FromPartItem(PartItem x)
        {
            return new PartListTableLineItem
            {
                Index = 0,
                ProcessArea = x.ProcessArea,
                FunctionalGroup = x.FunctionalGroup,
                FunctionalElement = x.GetFunctionalElement(),
                MaterialNo = x.DesignMaterial?.MaterialNo ?? string.Empty,
                Description = x.Description,
                Specification = x.DesignMaterial?.Specifications ?? string.Empty,
                TechnicalDataChinese = x.DesignMaterial?.TechnicalData ?? string.Empty,
                TechnicalDataEnglish = x.DesignMaterial?.TechnicalDataEnglish ?? string.Empty,
                Total = x.SubTotal,
                InGroup = x.SubTotal,
                Count = x.SubTotal,
                Unit = x.DesignMaterial?.Unit ?? string.Empty,
                Supplier = x.DesignMaterial?.Supplier ?? string.Empty,
                ManufacturerMaterialNo = x.DesignMaterial?.ManufacturerMaterialNumber ?? string.Empty,
                Type = x.DesignMaterial?.Type ?? string.Empty,
                Classification = string.Empty,
                Attachment = string.Empty
            };
        }

        /// <summary>
        ///     Create a copy of <see cref="PartListTableLineItem" /> and reset its designations.
        ///     Used for virtual part items.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="targetFunctionalGroup"></param>
        /// <returns></returns>
        public static PartListTableLineItem CopyTo(PartListTableLineItem x, string targetFunctionalGroup)
        {
            return new PartListTableLineItem
            {
                ProcessArea = x.ProcessArea,
                FunctionalGroup = targetFunctionalGroup,
                FunctionalElement = x.FunctionalElement.Replace(x.FunctionalGroup, targetFunctionalGroup),
                MaterialNo = x.MaterialNo,
                Description = x.Description,
                Specification = x.Specification,
                TechnicalDataChinese = x.TechnicalDataChinese,
                TechnicalDataEnglish = x.TechnicalDataEnglish,
                Total = x.Total,
                InGroup = x.InGroup,
                Count = x.Count,
                Unit = x.Unit,
                Supplier = x.Supplier,
                ManufacturerMaterialNo = x.ManufacturerMaterialNo,
                Type = x.Type,
                Classification = x.Classification,
                Attachment = x.Attachment
            };
        }

        #region Properties

        /// <summary>
        ///     序号
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        ///     区域号
        /// </summary>
        public string ProcessArea { get; set; } = string.Empty;

        /// <summary>
        ///     功能组
        /// </summary>
        public string FunctionalGroup { get; set; } = string.Empty;

        /// <summary>
        ///     功能元件
        /// </summary>
        public string FunctionalElement { get; set; } = string.Empty;

        /// <summary>
        ///     物料号
        /// </summary>
        public string MaterialNo { get; set; } = string.Empty;

        /// <summary>
        ///     描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        ///     规格
        /// </summary>
        public string Specification { get; set; } = string.Empty;

        /// <summary>
        ///     技术参数-中文
        /// </summary>
        public string TechnicalDataChinese { get; set; } = string.Empty;

        /// <summary>
        ///     技术参数-英文
        /// </summary>
        public string TechnicalDataEnglish { get; set; } = string.Empty;

        /// <summary>
        ///     数量
        /// </summary>
        public double Count { get; set; }

        /// <summary>
        ///     总数量
        /// </summary>
        public double Total { get; set; }

        /// <summary>
        ///     组内数量
        /// </summary>
        public double InGroup { get; set; }

        /// <summary>
        ///     单位
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        ///     供应商
        /// </summary>
        public string Supplier { get; set; } = string.Empty;

        /// <summary>
        ///     制造商物品编号
        /// </summary>
        public string ManufacturerMaterialNo { get; set; } = string.Empty;

        /// <summary>
        ///     型号
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        ///     分类
        /// </summary>
        public string Classification { get; set; } = string.Empty;

        /// <summary>
        ///     附件
        /// </summary>
        public string Attachment { get; set; } = string.Empty;

        #endregion
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
                MaterialNo = string.IsNullOrEmpty(m.MaterialNo) ? Guid.NewGuid().ToString() : m.MaterialNo,
                m.FunctionalGroup
            })
            .Select(group => new
            {
                group.Key.MaterialNo,
                group.Key.FunctionalGroup,
                CountInGroup = group.Sum(m => m.Count),
                Total = partListItems.Where(m => m.MaterialNo == group.Key.MaterialNo).Sum(m => m.Count)
            });

        foreach (var group in grouped)
        foreach (var material in partListItems.Where(m =>
                     m.MaterialNo == group.MaterialNo && m.FunctionalGroup == group.FunctionalGroup))
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