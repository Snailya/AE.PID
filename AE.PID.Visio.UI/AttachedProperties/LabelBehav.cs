using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
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
            WrapControlWithGridAndLabel(inputElement);
        else
            // remove prev value
            UnwrapControl(inputElement);
    }

    private static void WrapControlWithGridAndLabel(Control control)
    {
        if (control.GetVisualParent() is not Panel parent) return;

        // 创建一个新的 Grid
        var grid = new Grid();

        // 定义两列，第一列用于 Label，第二列用于原控件
        grid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = GridLength.Auto, SharedSizeGroup = ValueProperty.Name });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

        // 创建一个 TextBlock，用于显示 LabelProperty 的值
        var label = new TextBlock
        {
            Text = GetValue(control),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        Grid.SetColumn(label, 0); // 将 TextBlock 放在第一列
        grid.Children.Add(label);

        // 获取控件在父容器中的索引
        var index = parent.Children.IndexOf(control);

        // 从原来的父容器移除
        parent.Children.Remove(control);
        // 将原控件放在第二列
        Grid.SetColumn(control, 1);
        grid.Children.Add(control);

        // 将包裹后的控件放回父容器
        parent.Children.Insert(index, grid);
        
        control.AttachedToLogicalTree += OnAttachedToLogicalTree;
    }

    private static void OnAttachedToLogicalTree(object sender, LogicalTreeAttachmentEventArgs args)
    {
        if (sender is TextBox { Parent: Grid grid } textBox)
        {
            grid.Margin = textBox.Margin;
            textBox.Margin = new Thickness(0);
        }
    }

    private static void UnwrapControl(Control control)
    {
        var grid = control.GetVisualParent() as Grid;
        if (grid == null || !grid.Children.Contains(control)) return;

        var parent = grid.GetVisualParent() as Panel;
        if (parent == null) return;

        // 获取 Grid 在父容器中的索引
        var index = parent.Children.IndexOf(grid);

        // 移除 Grid，恢复原始控件
        parent.Children.RemoveAt(index);
        parent.Children.Insert(index, control);
    }
}