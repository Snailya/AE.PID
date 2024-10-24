using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace AE.PID.Visio.UI.Avalonia.AttachedProperties;

public class LabelBehav
{
    public static readonly AttachedProperty<string> ValueProperty =
        AvaloniaProperty.RegisterAttached<LabelBehav, Control, string>("Value");

    static LabelBehav()
    {
        ValueProperty.Changed.AddClassHandler<Control>(HandleLabelChanged);
    }

    public static void SetValue(Control obj, string value)
    {
        obj.SetValue(ValueProperty, value);
    }

    public static string GetValue(Control obj)
    {
        return obj.GetValue(ValueProperty);
    }

    private static void HandleLabelChanged(Control inputElement, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is string)
            // Add non-null value
            Wrap(inputElement);
        else
            // remove prev value
            Unwrap(inputElement);
    }

    private static void Wrap(Control control)
    {
        if (control.GetVisualParent() is not Control parent) return;
        if (parent.Name is { } s && s.StartsWith("Attach")) return;

        // 创建一个新的 Grid
        var wrapper = new Grid
        {
            Name = $"Attach_{Guid.NewGuid()}" // this name is used to avoid circular loop when the parent is also a grid.
        };

        // 定义两列，第一列用于 Label，第二列用于原控件
        wrapper.ColumnDefinitions.Add(new ColumnDefinition
            { Width = GridLength.Auto, SharedSizeGroup = ValueProperty.Name });
        wrapper.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

        // 创建一个 TextBlock，用于显示 LabelProperty 的值
        var label = new TextBlock
        {
            Text = GetValue(control),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            FontSize = control switch
            {
                // because sometimes the input element has custom fontsize, make the attached label with the same size
                TextBox textBox => textBox.FontSize,
                TextBlock textBlock => textBlock.FontSize,
                _ => TextBlock.FontSizeProperty.GetDefaultValue(typeof(double))
            },
            FontWeight = FontWeight.Bold
        };
        
        Grid.SetColumn(label, 0); // 将 TextBlock 放在第一列
        wrapper.Children.Add(label);

        switch (parent)
        {
            case Panel panel:
            {
                var index = panel.Children.IndexOf(control);

                // 从原来的父容器移除
                panel.Children.Remove(control);
                // 将原控件放在第二列
                Grid.SetColumn(control, 1);
                wrapper.Children.Add(control);

                // 将包裹后的控件放回父容器
                panel.Children.Insert(index, wrapper);
                break;
            }
            case ContentPresenter contentPresenter:
                // 从原来的父容器移除
                contentPresenter.Content = null;

                // 将原控件放在第二列
                Grid.SetColumn(control, 1);
                wrapper.Children.Add(control);

                contentPresenter.Content = wrapper;
                break;
            case Decorator decorator:
                // 从原来的父容器移除
                decorator.Child = null;

                Grid.SetColumn(control, 1);
                wrapper.Children.Add(control);

                decorator.Child = wrapper;
                break;
        }

        control.AttachedToLogicalTree += OnAttachedToLogicalTree;
    }

    private static void OnAttachedToLogicalTree(object sender, LogicalTreeAttachmentEventArgs args)
    {
        if (sender is Control { Parent: Grid grid } control)
        {
            grid.Margin = control.Margin;
            control.Margin = new Thickness(0);
        }
    }

    private static void Unwrap(Control control)
    {
        if (control.GetVisualParent() is not Grid grid || !grid.Children.Contains(control)) return;

        var parent = grid.GetVisualParent();
        grid.Children.Remove(control);


        switch (parent)
        {
            case Panel panel:
            {
                // 获取 Grid 在父容器中的索引
                var index = panel.Children.IndexOf(grid);

                // 移除 Grid，恢复原始控件
                panel.Children.RemoveAt(index);
                panel.Children.Insert(index, control);
                break;
            }
            case ContentPresenter contentPresenter:
                contentPresenter.Content = control;
                break;
            case Decorator decorator:
                decorator.Child = control;
                break;
        }
    }
}