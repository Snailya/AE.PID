using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using ClosedXML;
using ClosedXML.Attributes;

namespace AE.PID.Client.Core;

public class PartListItem : IDataRow
{
    private static readonly ConcurrentDictionary<int, PropertyInfo> Cache = new();

    public string Category { get; set; }

    /// <summary>
    ///     序号
    /// </summary>
    [property: XLColumn(Order = 1)]
    public int Index { get; set; }

    /// <summary>
    ///     区域号
    /// </summary>
    [property: XLColumn(Order = 2)]
    public string ProcessArea { get; set; } = string.Empty;

    /// <summary>
    ///     功能组
    /// </summary>
    [property: XLColumn(Order = 3)]
    public string FunctionalGroup { get; set; } = string.Empty;

    /// <summary>
    ///     功能元件
    /// </summary>
    [property: XLColumn(Order = 4)]
    public string FunctionalElement { get; set; } = string.Empty;

    /// <summary>
    ///     物料号
    /// </summary>
    [property: XLColumn(Order = 5)]
    public string MaterialNo { get; set; } = string.Empty;

    /// <summary>
    ///     描述
    /// </summary>
    [property: XLColumn(Order = 6)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     规格
    /// </summary>
    [property: XLColumn(Order = 7)]
    public string Specification { get; set; } = string.Empty;

    /// <summary>
    ///     技术参数-中文
    /// </summary>
    [property: XLColumn(Order = 8)]
    public string TechnicalDataChinese { get; set; } = string.Empty;

    /// <summary>
    ///     技术参数-英文
    /// </summary>
    [property: XLColumn(Order = 9)]
    public string TechnicalDataEnglish { get; set; } = string.Empty;


    /// <summary>
    ///     总数量
    /// </summary>
    [property: XLColumn(Order = 10)]
    public double Total { get; set; }

    /// <summary>
    ///     组内数量
    /// </summary>
    [property: XLColumn(Order = 11)]
    public double InGroup { get; set; }

    /// <summary>
    ///     数量
    /// </summary>
    [property: XLColumn(Order = 12)]
    public double Count { get; set; }
    
    /// <summary>
    ///     单位
    /// </summary>
    [property: XLColumn(Order = 13)]
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    ///     供应商
    /// </summary>
    [property: XLColumn(Order = 14)]
    public string Supplier { get; set; } = string.Empty;

    /// <summary>
    ///     制造商物品编号
    /// </summary>
    [property: XLColumn(Order = 15)]
    public string ManufacturerMaterialNo { get; set; } = string.Empty;

    /// <summary>
    ///     型号
    /// </summary>
    [property: XLColumn(Order = 16)]
    public string OrderType { get; set; } = string.Empty;

    /// <summary>
    ///     分类
    /// </summary>
    [property: XLColumn(Order = 17)]
    public string Classification { get; set; } = string.Empty;

    /// <summary>
    ///     附件
    /// </summary>
    [property: XLColumn(Order = 18)]
    public string Attachment { get; set; } = string.Empty;

    public object[] ToDataRow(int? index = null)
    {
        if (index.HasValue)
            Index = index.Value;

        if (Cache.IsEmpty)
        {
            // initialize the cache
            var properties = GetType().GetProperties();
            foreach (var propertyInfo in properties)
                if (propertyInfo.HasAttribute<XLColumnAttribute>())
                {
                    var attribute = propertyInfo.GetCustomAttribute<XLColumnAttribute>();
                    if (attribute.Ignore != true)
                        Cache.TryAdd(attribute.Order, propertyInfo);
                }
        }

        // 找出属性中最大索引
        var maxIndex = Cache.Max(i => i.Key);

        // 根据最大索引创建数组
        var result = new object[maxIndex];

        foreach (var keyValuePair in Cache)
        {
            var value = keyValuePair.Value.GetValue(this);
            result[keyValuePair.Key - 1] = value ?? "";
        }

        return result;
    }
}