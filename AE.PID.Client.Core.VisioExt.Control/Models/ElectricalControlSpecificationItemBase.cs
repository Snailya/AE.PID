using System.Collections.Concurrent;
using System.Reflection;
using AE.PID.Client.Core.VisioExt.Control.SocketsAndLightings;
using ClosedXML;
using ClosedXML.Attributes;
using Force.DeepCloner;

namespace AE.PID.Client.Core.VisioExt.Control;

public abstract class ElectricalControlSpecificationItemBase : IDataRow
{
    private static readonly ConcurrentDictionary<Type, Dictionary<int, PropertyInfo>> Cache = new();

    /// <summary>
    ///     类别，用于确定属于哪个表格
    /// </summary>
    [property: XLColumn(Ignore = true)]
    public abstract Type Type { get; }

    /// <summary>
    ///     序号
    /// </summary>
    [property: XLColumn(Order = 1)]
    public int Index { get; set; }

    /// <summary>
    ///     工艺区域
    /// </summary>
    [property: XLColumn(Order = 3)]
    [ShapeSheetCell(CellDict.FunctionZone)]
    public string Zone { get; set; } = string.Empty;

    /// <summary>
    ///     功能组
    /// </summary>
    [property: XLColumn(Order = 4)]
    [ShapeSheetCell(CellDict.FunctionGroup, true)]
    public string Group { get; set; } = string.Empty;

    /// <summary>
    ///     所处功能段描述
    /// </summary>
    [property: XLColumn(Order = 5)]
    [ShapeSheetCell(CellDict.FunctionGroupDescription)]
    public string GroupDescription { get; set; } = string.Empty;

    /// <summary>
    ///     设备代号
    /// </summary>
    [property: XLColumn(Order = 6)]
    [ShapeSheetCell(CellDict.FunctionElement, true)]
    public string Designation { get; set; } = string.Empty;

    /// <summary>
    ///     设备功能描述
    /// </summary>
    [property: XLColumn(Order = 7)]
    [ShapeSheetCell(CellDict.Description)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     电控编号
    /// </summary>
    [property: XLColumn(Order = 8)]
    public virtual string FullDesignation => $"={Zone}++{Group}+{Group}-{Designation}";

    /// <summary>
    ///     数量
    /// </summary>
    [property: XLColumn(Order = 9)]
    [ShapeSheetCell(CellDict.Quantity)]
    public int Quantity { get; set; }

    public virtual object[] ToDataRow(int? index = null)
    {
        if (index.HasValue)
            Index = index.Value;

        if (!Cache.TryGetValue(Type, out var dict))
        {
            dict = new Dictionary<int, PropertyInfo>();

            // initialize the cache
            var properties = GetType().GetProperties();
            foreach (var propertyInfo in properties)
                if (propertyInfo.HasAttribute<XLColumnAttribute>())
                {
                    var attribute = propertyInfo.GetCustomAttribute<XLColumnAttribute>();
                    if (attribute.Ignore != true)
                        dict.Add(attribute.Order, propertyInfo);
                }

            Cache.TryAdd(Type, dict);
        }

        // 找出属性中最大索引
        var maxIndex = dict.Max(i => i.Key);

        // 根据最大索引创建数组
        var result = new object[maxIndex];

        int? designationIndex = null;
        foreach (var keyValuePair in dict)
        {
            if (designationIndex == null && keyValuePair.Value.Name == nameof(Designation))
                designationIndex = keyValuePair.Key;

            var value = keyValuePair.Value.GetValue(this);

            if (value == null)
            {
                result[keyValuePair.Key - 1] = "";
                continue;
            }

            // if the value
            var targetType = keyValuePair.Value.PropertyType;
            if (targetType.IsGenericType &&
                targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                targetType = Nullable.GetUnderlyingType(targetType)!;

            if (targetType == typeof(bool))
                result[keyValuePair.Key - 1] = (bool)value ? "Y" : "";
            else
                result[keyValuePair.Key - 1] = value;
        }

        return result;
    }

    public IEnumerable<ElectricalControlSpecificationItemBase> Flatten()
    {
        if (Quantity == 1
            || Type == typeof(Lighting)
            || Type == typeof(Socket)
            || (Type == typeof(Instrument) &&
                Designation.StartsWith("BG"))) // 位置开关，在Visio中有可能使用仪表对象表示（首字母BG），也可能直接使用接近开关表示
            return [this];

        var list = new List<ElectricalControlSpecificationItemBase>();
        for (var i = 0; i < Quantity; i++)
        {
            var item = this.DeepClone();

            item.Designation += (char)('A' + i);
            item.Quantity = 1;

            list.Add(item);
        }

        return list;
    }
}