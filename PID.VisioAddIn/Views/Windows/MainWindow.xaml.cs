using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
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

        Win32Ext.GetWindowRect(_helper!.Owner, out var rect);

        var parentLeft = rect.Left;
        var parentTop = rect.Top;
        var parentWidth = rect.Right - rect.Left;
        var parentHeight = rect.Bottom - rect.Top;

        Left = parentLeft + (parentWidth - ActualWidth) / 2;
        Top = parentTop + (parentHeight - ActualHeight) / 2;
    }
}