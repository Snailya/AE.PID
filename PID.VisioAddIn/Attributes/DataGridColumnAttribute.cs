using System;

namespace AE.PID.Attributes;

/// <summary>
///     The column name that displayed for this property when render Data Grid
/// </summary>
/// <param name="name"></param>
[AttributeUsage(AttributeTargets.Property)]
public class DataGridColumnNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

/// <summary>
///     The name path of the column and value path used for render Data Grid
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DataGridColumnAttribute(string namePath, string valuePath) : Attribute
{
    public string NamePath { get; } = namePath;
    public string ValuePath { get; } = valuePath;
}

/// <summary>
///     Indicates this property is used for generate multiple columns when render Data Grid.
///     The type argument for this property must has <see cref="DataGridColumnAttribute" />
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class DataGridMultipleColumnsAttribute : Attribute;