using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using AE.PID.Client.Core;
using AE.PID.Client.Core.VisioExt.Control;
using Microsoft.Office.Interop.Visio;
using Splat;

namespace AE.PID.Client.VisioAddIn;

public static class ControlExt
{
    private static readonly ConcurrentDictionary<string, Type> TypeRegistry = new();

    // 属性映射缓存（提升反射性能）
    private static readonly ConcurrentDictionary<Type, Dictionary<PropertyInfo, Attribute>> PropCache = new();

    // 初始化时扫描所有程序集（按需调整扫描范围）
    static ControlExt()
    {
        ScanAssemblies(AppDomain.CurrentDomain.GetAssemblies());
    }

    /// <summary>
    ///     Create a <see cref="ElectricalControlSpecificationItemBase" /> from Visio <see cref="Shape" />
    /// </summary>
    /// <param name="shape"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static ElectricalControlSpecificationItemBase? CreateFromShape(this Shape shape)
    {
        if (shape.Master == null) throw new ArgumentException("Master is null");

        if (!TypeRegistry.TryGetValue(shape.Master.BaseID, out var targetType))
        {
            LogHost.Default.Warn($"未注册的对象ID: {shape.Master.BaseID}");
            return null;
        }

        if (!Verify(shape)) return null;

        var instance = (ElectricalControlSpecificationItemBase)Activator.CreateInstance(targetType)!;
        PopulateData(instance, shape);
        return instance;
    }

    private static bool Verify(Shape shape)
    {
        // if it is a one d object, which means it is either a signal or a pipeline, no need to check its designation
        if (shape.OneD == (short)VBABool.True) return true;

        // if the quantity is zero, it is not a valid control item
        var quantity = shape.TryGetValue<double>(CellDict.Quantity);
        if (quantity is null or 0) return false;

        // only the element with 2 digital numbers can be treated as a valid control item
        var designation = shape.TryGetValue(CellDict.FunctionElement);
        if (string.IsNullOrEmpty(designation)) return false;
        var elementNumber = Regex.Match(designation, @"\d+").Value;
        return elementNumber.Length == 2;
    }

    // 数据填充逻辑
    private static void PopulateData(object instance, Shape shape)
    {
        var type = instance.GetType();

        // populate data by achieving data from cell
        var propMap = PropCache.GetOrAdd(type, t =>
        {
            var map = new Dictionary<PropertyInfo, Attribute>();
            foreach (var prop in t.GetProperties())
            {
                var attr = prop.GetCustomAttribute<ShapeSheetCell>() ?? prop.GetCustomAttribute<Callout>() ??
                    prop.GetCustomAttribute<Connected>() as Attribute;
                map[prop] = attr;
            }

            return map;
        });

        foreach (var prop in propMap)
            switch (prop.Value)
            {
                case ShapeSheetCell cellAttr:
                {
                    PopulateCellValue(instance, shape, prop.Key, cellAttr);
                    break;
                }
                case Callout calloutAttr:
                {
                    PopulateCalloutShape(instance, shape, prop.Key, calloutAttr);
                    break;
                }
                case Connected connectorAttr:
                {
                    PopulateConnectedShape(instance, shape, prop.Key, connectorAttr);
                    break;
                }
            }
    }

    private static void PopulateConnectedShape(object instance, Shape shape, PropertyInfo propertyInfo,
        Connected connectorAttr)
    {
        var connected = (shape.OneD == (short)VBABool.True ? shape.Connects : shape.FromConnects)
            .OfType<Connect>()
            .SelectMany(x => new[] { x.FromSheet, x.ToSheet })
            .Where(x => x.Master != null && x.ID != shape.ID)
            .Where(x => connectorAttr.Includes == null || connectorAttr.Includes.Contains(x.Master.BaseID))
            .Where(x => connectorAttr.Excepts == null || !connectorAttr.Excepts.Contains(x.Master.BaseID))
            .Select(x => x.CreateFromShape())
            .Where(x => x != null)
            .ToList();

        if (IsCollection(propertyInfo))
        {
            propertyInfo.SetValue(instance, connected);
        }
        else
        {
            var value = connected.FirstOrDefault();
            propertyInfo.SetValue(instance, value);
        }
    }

    private static void PopulateCalloutShape(object instance, Shape shape, PropertyInfo propertyInfo,
        Callout callout)
    {
        var calloutShape = shape.CalloutsAssociated.OfType<int>()
            .Select(x => shape.ContainingPage.Shapes.ItemFromID[x])
            .Where(x => x.Master != null && x.Master.BaseID == callout.BaseId);
        if (IsCollection(propertyInfo))
        {
            var value = calloutShape.Select(x => x.CreateFromShape());
            propertyInfo.SetValue(instance, value);
        }
        else
        {
            var value = calloutShape.FirstOrDefault()?.CreateFromShape();
            propertyInfo.SetValue(instance, value);
        }
    }

    private static void PopulateCellValue(object instance, Shape shape, PropertyInfo propertyInfo,
        ShapeSheetCell shapeSheetCell)
    {
        var targetType = propertyInfo.PropertyType;

        // 根据Attribute判断使用格式化结果，还是值
        var value = shapeSheetCell.UseFormatValue
            ? shape.TryGetFormatValue(shapeSheetCell.CellName)
            : shape.TryGetValue(shapeSheetCell.CellName);

        // 如果是空值
        if (string.IsNullOrEmpty(value))
        {
            propertyInfo.SetValue(instance, null);
            return;
        }

        // 如果不是空值，尝试正则提取需要的部分
        if (shapeSheetCell.Regex != null) value = shapeSheetCell.Regex.Match(value).Value;

        // 如果是空值
        if (string.IsNullOrEmpty(value))
        {
            propertyInfo.SetValue(instance, null);
            return;
        }

        // 如果是Nullable泛型
        if (targetType.IsGenericType &&
            targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            targetType = Nullable.GetUnderlyingType(targetType)!;

        // 如果目标类型是字符串，直接赋值
        if (targetType == typeof(string))
            propertyInfo.SetValue(instance, value);
        else
            try
            {
                // 2025.3.5: 如果待转换为的类型时int，需要先cut掉小数部分。
                if (targetType == typeof(int)) value = value!.Split('.')[0];

                var convertedValue = Convert.ChangeType(value, targetType);
                propertyInfo.SetValue(instance, convertedValue);
            }
            catch
            {
                // 可记录日志或忽略错误
            }
    }

    // 程序集扫描注册
    private static void ScanAssemblies(IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies)
        foreach (var type in assembly.GetTypes())
        {
            var attr = type.GetCustomAttribute<ElectricalControlSpecificationItem>();
            if (attr?.BaseIDs == null) continue;

            foreach (var id in attr.BaseIDs)
                TypeRegistry.AddOrUpdate(id,
                    type,
                    (_, existingType) => throw new InvalidOperationException(
                        $"ID冲突: {attr.BaseIDs} 已绑定到 {existingType.Name}"));
        }
    }

    public static bool IsCollection(PropertyInfo propertyInfo)
    {
        // 获取属性类型
        var propertyType = propertyInfo.PropertyType;

        // 排除字符串类型
        if (propertyType == typeof(string)) return false;

        // 检查是否实现了 IEnumerable 接口
        return typeof(IEnumerable).IsAssignableFrom(propertyType);
    }
}