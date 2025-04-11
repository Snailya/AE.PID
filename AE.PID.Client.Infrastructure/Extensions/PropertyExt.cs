using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AE.PID.Client.Infrastructure;

internal static class PropertyExt
{
    public static void UpdateValue<T, TValue>(this T target, Expression<Func<T, TValue>> memberExpression,
        TValue newValue)
    {
        // 解包可能的类型转换表达式
        var expr = memberExpression.Body;

        // 处理Convert/ConvertChecked表达式（值类型装箱、枚举转换等场景）
        if (expr is UnaryExpression
            {
                NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked
            } unaryExpression)
            expr = unaryExpression.Operand;

        if (expr is MemberExpression memberExpr)
        {
            // Traverse to the final object and member
            var (finalTarget, member) = GetFinalTargetAndMember(target, memberExpr);

            object? valueToSet = newValue;

            switch (member)
            {
                // Update the property value
                case PropertyInfo property:
                    valueToSet = ConvertValueToPropertyType(valueToSet, property.PropertyType);
                    property.SetValue(finalTarget, valueToSet);
                    break;
                // Update the field value
                case FieldInfo field:
                    field.SetValue(finalTarget, valueToSet);
                    break;
                default:
                    throw new InvalidOperationException("MemberExpression must target a property or field.");
            }
        }
        else
        {
            throw new InvalidOperationException("Expression must be a MemberExpression (may wrapped in Convert).");
        }
    }

    // private static (object? FinalTarget, MemberInfo Member) GetFinalTargetAndMember(object? target,
    //     MemberExpression expr)
    // {
    //     // Stack to keep track of the member chain
    //     var memberStack = new Stack<MemberInfo>();
    //     Expression currentExpr = expr;
    //
    //     while (currentExpr is MemberExpression memberExpr)
    //     {
    //         memberStack.Push(memberExpr.Member);
    //         currentExpr = memberExpr.Expression;
    //     }
    //
    //     // 遍历成员访问链获取最终对象
    //     var currentTarget = target;
    //     foreach (var member in memberStack)
    //         currentTarget = member switch
    //         {
    //             PropertyInfo prop => prop.GetValue(currentTarget),
    //             FieldInfo field => field.GetValue(currentTarget),
    //             _ => throw new InvalidOperationException("Unsupported member type in expression.")
    //         };
    //
    //     // 最后一个成员是实际要设置的成员
    //     var finalMember = memberStack.Pop();
    //     return (currentTarget, finalMember);
    // }

    private static (object? FinalTarget, MemberInfo Member) GetFinalTargetAndMember(object? root,
        MemberExpression memberExpr)
    {
        // Stack to keep track of the member chain
        var members = new Stack<MemberExpression>();
        while (memberExpr != null)
        {
            members.Push(memberExpr);
            memberExpr = memberExpr.Expression as MemberExpression;
        }

        // Evaluate the intermediate objects
        var currentTarget = root;
        while (members.Count > 1) // Stop at the second-to-last member
        {
            var currentMember = members.Pop();
            var member = currentMember.Member;

            currentTarget = member switch
            {
                PropertyInfo property => property.GetValue(currentTarget),
                FieldInfo field => field.GetValue(currentTarget),
                _ => throw new InvalidOperationException("Unsupported member type.")
            };

            if (currentTarget == null) throw new NullReferenceException("Intermediate member is null.");
        }

        return (currentTarget, members.Pop().Member);
    }

    private static object? ConvertValueToPropertyType(object? value, Type targetType)
    {
        // 空值直接返回
        if (value == null) return null;

        // 类型完全匹配直接返回
        if (targetType.IsInstanceOfType(value)) return value;

        /******************** 集合类型处理 ********************/
        if (typeof(IEnumerable).IsAssignableFrom(targetType) && value is IEnumerable enumerableValue)
        {
            // 获取目标集合元素类型
            var elementType = GetEnumerableElementType(targetType);

            // 创建兼容的集合类型
            return CreateCompatibleCollection(enumerableValue, targetType, elementType);
        }

        /******************** 其他类型转换 ********************/
        // 此处可添加更多类型转换逻辑（如字符串转DateTime等）

        // 最后尝试强制类型转换
        try
        {
            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            throw new InvalidCastException(
                $"Cannot convert type {value.GetType()} to {targetType}");
        }
    }

    /// <summary>
    ///     获取集合元素类型（支持数组、泛型集合、非泛型集合）
    /// </summary>
    private static Type GetEnumerableElementType(Type collectionType)
    {
        // 处理数组类型
        if (collectionType.IsArray)
            return collectionType.GetElementType();

        // 处理泛型集合（IEnumerable<T>）
        if (collectionType.IsGenericType &&
            collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return collectionType.GetGenericArguments()[0];

        // 处理实现IEnumerable<T>的集合
        var interfaceType = collectionType.GetInterfaces()
            .FirstOrDefault(t => t.IsGenericType &&
                                 t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return interfaceType?.GetGenericArguments()[0];
    }

    /// <summary>
    ///     创建目标类型的兼容集合
    /// </summary>
    private static object CreateCompatibleCollection(IEnumerable source, Type targetType, Type elementType)
    {
        // 类型检查开关
        var requiresExactType = targetType.IsArray ||
                                targetType is { IsGenericType: true, IsInterface: false };

        return requiresExactType ? CreateConcreteCollection() : source;

        // 创建具体集合实例
        object CreateConcreteCollection()
        {
            if (targetType.IsArray)
            {
                var list = source.Cast<object?>().ToList();
                var array = Array.CreateInstance(elementType, list.Count);
                Array.Copy(list.ToArray(), array, list.Count);
                return array;
            }

            if (!targetType.IsGenericType)
                throw new NotSupportedException($"Unsupported collection type: {targetType}");
            var genericDef = targetType.GetGenericTypeDefinition();
            if (genericDef == typeof(List<>))
            {
                var listType = typeof(List<>).MakeGenericType(elementType);
                return Activator.CreateInstance(listType, source);
            }

            if (genericDef != typeof(Collection<>))
                throw new NotSupportedException($"Unsupported collection type: {targetType}");
            var collectionType = typeof(Collection<>).MakeGenericType(elementType);
            var collection = Activator.CreateInstance(collectionType);
            var addMethod = collectionType.GetMethod("Add");
            foreach (var item in source)
                if (addMethod != null)
                    addMethod.Invoke(collection, [item]);
            return collection;
        }
    }
}