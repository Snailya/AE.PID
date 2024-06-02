using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using AE.PID.Tools;

namespace AE.PID.Views.Windows;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : WindowBase
{
    private WindowInteropHelper? _helper;

    public MainWindow()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            _helper = new WindowInteropHelper(this)
            {
                Owner = new IntPtr(Globals.ThisAddIn.Application.WindowHandle32)
            };
        };
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
}