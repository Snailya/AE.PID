using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using AE.PID.Attributes;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Tools;

internal static class UiHelper
{
    /// <summary>
    ///     Create a visio window to hold visual element.
    /// </summary>
    /// <param name="caption"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public static IntPtr CreateAnchorBarAddonWithUserControl(string caption,
        UserControl content)
    {
        try
        {
            // Create a visio window to hold the visual, this is not necessary as without this we still could create a visual element using HwndSource class. However, to make the visual element not a standalone window, we need to get the Hwnd of this parent window, and specify the WS_CHILD value for window style.
            var state = VisWindowStates.visWSVisible | VisWindowStates.visWSFloating;
            var types = VisWinTypes.visAnchorBarAddon;
            // Notice that the nWidth and nHeight parameters are not necessary. If they are specified as 0, it will decide by the system. 
            var parent =
                Globals.ThisAddIn.Application.ActiveWindow.Windows.Add(caption, state, types, 8, 10, 0, 0, 0, 0, 0);

            var parameters = new HwndSourceParameters(caption)
            {
                ParentWindow = (IntPtr)parent.WindowHandle32,
                // Notice if WS_CHILD is missing, the visual will not display as a child of the parent window.
                WindowStyle = NativeMethods.WS_VISIBLE | NativeMethods.WS_CHILD
            };
            parameters.SetSize((int)content.Width, (int)content.Height);


            // Make sure we set the parent successfully
            Debug.Assert(parameters.ParentWindow == (IntPtr)parent.WindowHandle32);

            var source = new HwndSource(parameters)
            {
                RootVisual = content,
                SizeToContent = SizeToContent.WidthAndHeight
            };

            source.ContentRendered += (sender, _) =>
            {
                if (sender is HwndSource)
                {
                }
            };

            return source.Handle;
        }
        catch (Exception)
        {
            Debugger.Break();
            throw;
        }
    }

    public static T? FindParent<T>(this DependencyObject child) where T : DependencyObject
    {
        while (true)
        {
            var parentObject = VisualTreeHelper.GetParent(child);

            switch (parentObject)
            {
                case null:
                    return null;
                case T parent:
                    return parent;
                default:
                    child = parentObject;
                    break;
            }
        }
    }

    public static T? FindVisualChild<T>(this DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T dependencyObject) return dependencyObject;

            var childOfChild = FindVisualChild<T>(child);
            if (childOfChild != null)
                return childOfChild;
        }

        return null;
    }

    public static void PopulateColumns(this DataGrid grid)
    {
        grid.Columns.Clear();

        if (grid.Items.Count <= 0) return;

        var seed = grid.Items[0];

        var properties = seed.GetType().GetProperties();

        // add property with DtaGridColumnAttribute
        foreach (var property in properties.Where(x => x.GetCustomAttribute<DataGridColumnNameAttribute>() != null))
        {
            if (property.GetCustomAttribute<DataGridColumnNameAttribute>() is not { } columnNameAttribute) continue;
            var name = columnNameAttribute.Name;
            grid.Columns.Add(new DataGridTextColumn
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
                    grid.Columns.Add(new DataGridTextColumn
                        { Header = columnName, Binding = binding });
                index++;
            }
        }
    }
}