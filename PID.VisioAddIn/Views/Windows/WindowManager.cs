using System;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using AE.PID.ViewModels;
using AE.PID.Views.Pages;

namespace AE.PID.Views.Windows;

public class WindowManager
{
    private readonly MainWindow _mainWindow = new();
    private readonly SideWindow _sideWindow = new();

    public WindowManager()
    {
        _mainWindow.Title = Assembly.GetExecutingAssembly().GetName().Name;

        _ = new WindowInteropHelper(_mainWindow)
        {
            Owner = new IntPtr(Globals.ThisAddIn.Application.WindowHandle32)
        };

        _mainWindow.LocationChanged += (_, _) =>
        {
            if (_sideWindow.Visibility == Visibility.Visible)
            {
                _sideWindow.Top = _mainWindow.Top;
                _sideWindow.Left = _mainWindow.Left + _mainWindow.Width;
            }
        };

        _mainWindow.SizeChanged += (_, _) =>
        {
            if (_sideWindow.Visibility == Visibility.Visible)
            {
                _sideWindow.Top = _mainWindow.Top;
                _sideWindow.Left = _mainWindow.Left + _mainWindow.Width;
            }
        };
    }

    public void Show<TViewModel>(PageBase<TViewModel> page) where TViewModel : ViewModelBase
    {
        _mainWindow.Content = page;
        _mainWindow.Show();
    }

    public void SideShow<TViewModel>(PageBase<TViewModel> page) where TViewModel : ViewModelBase
    {
        _sideWindow.Owner = _mainWindow;
        _sideWindow.Top = _mainWindow.Top;
        _sideWindow.Left = _mainWindow.Left + _mainWindow.Width;

        var binding = new Binding
        {
            Path = new PropertyPath("ActualHeight"),
            Source = _mainWindow,
            Mode = BindingMode.OneWay
        };
        _sideWindow.SetBinding(FrameworkElement.HeightProperty, binding);

        _sideWindow.Content = page;
        _sideWindow.Show();
    }
}