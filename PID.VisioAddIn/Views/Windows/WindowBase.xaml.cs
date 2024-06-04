using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using AE.PID.Tools;
using AE.PID.ViewModels;

namespace AE.PID.Views.Windows;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class WindowBase
{
    public enum WindowButton
    {
        CloseOnly,
        Normal,
        None
    }

    public static readonly DependencyProperty WindowButtonStyleProperty = DependencyProperty.Register(
        nameof(WindowButtonStyle), typeof(WindowButton), typeof(WindowBase),
        new PropertyMetadata(WindowButton.Normal));

    private WindowInteropHelper? _helper;

    public WindowButton WindowButtonStyle
    {
        get => (WindowButton)GetValue(WindowButtonStyleProperty);
        set => SetValue(WindowButtonStyleProperty, value);
    }

    protected override void OnContentRendered(System.EventArgs e)
    {
        SizeToContent = SizeToContent.WidthAndHeight;
        base.OnContentRendered(e);
    }

    protected override void OnActivated(System.EventArgs e)
    {
        base.OnActivated(e);
        SizeToContent = SizeToContent.Manual;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        foreach (Window ownedWindow in OwnedWindows) ownedWindow.Close();

        Hide();
        Content = null;

        e.Cancel = true;
    }

    public void CenterOwner()
    {
        if (_helper?.Owner == IntPtr.Zero) return;

        // GetWindowRect will return the rect in system resolution without scaling.
        // It means if I make a window full screen, it will return 1920 width even it is in 150 % DPI scaling for a screen resolution 1920x1080.
        Win32Ext.GetWindowRect(_helper!.Owner, out var rect);

        var parentCenterX = (rect.Right + rect.Left) / 2;
        var parentCenterY = (rect.Bottom + rect.Top) / 2;

        // note that this not consider the dpi scaling,
        // for example, the actual width is 320 px,
        // while a snip tool measure result is 320 * 1.5 when scaling is 150 %
        var width = (Content as UserControl)!.ActualWidth;
        var height = (Content as UserControl)!.ActualHeight;

        var dpiScale = VisualTreeHelper.GetDpi(this);

        // however, the position of the window in WPF uses dpi related position.
        // that is for a 150% dpi scaling monitor, the mid-screen is 1920 / 1.5 / 2 = 640
        Left = parentCenterX / dpiScale.DpiScaleX - width / 2;
        Top = parentCenterY / dpiScale.DpiScaleY - height / 2;
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not Button button) return;
        switch (button.Name)
        {
            case "PART_Minimize":
                WindowState = WindowState.Minimized;
                break;
            case "PART_Maximize":
                WindowState = WindowState.Maximized;
                break;
            case "PART_Close":
                Close();
                break;
        }
    }

    #region Constructors
    
    public WindowBase(IntPtr owner) : this()
    {
        // if the parent is a win32 window
        Loaded += (_, _) =>
        {
            _helper = new WindowInteropHelper(this)
            {
                Owner = owner
            };
        };
    }

    public WindowBase(Window owner) : this()
    {
        // if the parent is a WPF window
        Loaded += (_, _) => { Owner = owner; };
    }

    private WindowBase()
    {
        Title = Properties.Resources.PROPERTY_product_name;
        MaxHeight = SystemParameters.WorkArea.Height;
        MaxWidth = SystemParameters.WorkArea.Width;

        DataContext = new WindowViewModel(this);

        InitializeComponent();
    }

    #endregion
}