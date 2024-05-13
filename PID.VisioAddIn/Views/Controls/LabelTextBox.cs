using System.Windows;
using System.Windows.Controls;

namespace AE.PID.Views;

public class LabelTextBox : TextBox
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(LabelTextBox), new PropertyMetadata(string.Empty));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }
}