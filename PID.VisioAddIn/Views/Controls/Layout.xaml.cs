using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using ReactiveUI;

namespace AE.PID.Views.Controls;

/// <summary>
/// Interaction logic for DocumentInfo.xaml
/// </summary>
public partial class Layout : UserControl
{
    public Layout()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
    nameof(Header), typeof(string), typeof(Layout), new PropertyMetadata(default(string)));

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static readonly DependencyProperty FooterProperty = DependencyProperty.Register(nameof(Footer),
        typeof(object), typeof(Layout), (PropertyMetadata)new FrameworkPropertyMetadata((object)null));

    public object Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }
}