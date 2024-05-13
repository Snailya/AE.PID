using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AE.PID.Attributes;
using DynamicData.Binding;

namespace AE.PID.Views;

public class AutoColumnsDataGrid : DataGrid
{
    private IDisposable? _cleanup;

    public AutoColumnsDataGrid()
    {
        Loaded += (_, _) =>
        {
            _cleanup = Items.ObserveCollectionChanges()
                .Subscribe(_ => PopulateColumns());
        };

        Unloaded += (_, _) => { _cleanup?.Dispose(); };
    }

    private void PopulateColumns()
    {
        Columns.Clear();

        if (Items.Count <= 0) return;

        var seed = Items[0];

        var properties = seed.GetType().GetProperties();

        // add property with DtaGridColumnAttribute
        foreach (var property in properties.Where(x => x.GetCustomAttribute<DataGridColumnNameAttribute>() != null))
        {
            if (property.GetCustomAttribute<DataGridColumnNameAttribute>() is not { } columnNameAttribute) continue;
            var name = columnNameAttribute.Name;
            Columns.Add(new DataGridTextColumn
                { Header = name, Binding = new Binding(property.Name) });
        }

        // add property with DataGridColumnsAttribute
        foreach (var property in
                 properties.Where(x => x.GetCustomAttribute<DataGridMultipleColumnsAttribute>() != null))
        {
            Debug.Assert(property.PropertyType.IsGenericType);

            var typeArguments = property.PropertyType.GetGenericArguments()[0];

            if (typeArguments.GetCustomAttribute<DataGridColumnAttribute>() is not
                { } dataGridColumnAttribute) continue;

            if (property.GetValue(seed) is not IEnumerable items) continue;

            var index = 0;
            foreach (var item in items)
            {
                var binding = new Binding
                {
                    Path = new PropertyPath($"{property.Name}[{index}].{dataGridColumnAttribute.ValuePath}")
                };

                var columnName = item.GetType().GetProperty(dataGridColumnAttribute.NamePath)?.GetValue(item);
                if (columnName != null)
                    Columns.Add(new DataGridTextColumn
                        { Header = columnName, Binding = binding });
                index++;
            }
        }
    }
}