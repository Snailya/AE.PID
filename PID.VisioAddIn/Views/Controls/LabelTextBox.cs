using System.Windows;
using System.Windows.Controls;

namespace AE.PID.Views;

public class LabelTextBox : TextBox
{
    public static readonly DependencyProperty ErrorProperty = DependencyProperty.Register(
        nameof(Error), typeof(string), typeof(LabelTextBox), new PropertyMetadata(default(string)));

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(LabelTextBox), new PropertyMetadata(string.Empty));

    public string Error
    {
        get => (string)GetValue(ErrorProperty);
        set => SetValue(ErrorProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }
}