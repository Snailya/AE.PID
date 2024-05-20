using System;
using System.Globalization;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using AE.PID.ViewModels;
using AE.PID.Views;
using AE.PID.Views.Windows;
using ReactiveUI;

namespace AE.PID.Services;

public class WindowManager : IDisposable
{
    private static WindowManager? _instance;
    private readonly ChildWindow _childWindow = new();
    private readonly MainWindow _mainWindow = new();

    private WindowManager()
    {
        _mainWindow.Title = Assembly.GetExecutingAssembly().GetName().Name;


        _mainWindow.LocationChanged += (_, _) =>
        {
            if (_childWindow.Visibility != Visibility.Visible) return;

            _childWindow.Top = _mainWindow.Top;
            _childWindow.Left = _mainWindow.Left + _mainWindow.Width;
        };

        _mainWindow.SizeChanged += (_, _) =>
        {
            if (_childWindow.Visibility != Visibility.Visible) return;

            _childWindow.Top = _mainWindow.Top;
            _childWindow.Left = _mainWindow.Left + _mainWindow.Width;
        };
    }

    public static Dispatcher? Dispatcher { get; private set; }

    public void Dispose()
    {
        Dispatcher?.Invoke(() =>
        {
            _childWindow.Close();
            _mainWindow.Close();
        });

        Dispatcher?.InvokeShutdown();
    }

    public static WindowManager? GetInstance()
    {
        return _instance;
    }

    public static void Initialize()
    {
        _instance = new WindowManager();

        // initialize dispatcher
        Dispatcher = Dispatcher.CurrentDispatcher;
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;

        Dispatcher.Run();
    }

    public void SetContent<TMain, TSide>(PageBase<TMain> main, PageBase<TSide> child)
        where TMain : ViewModelBase where TSide : ViewModelBase
    {
        _mainWindow.Content = main;
        _childWindow.Content = child;
    }

    public void SetContent<TMain>(PageBase<TMain> main)
        where TMain : ViewModelBase
    {
        _mainWindow.Content = main;
        _childWindow.Content = null;
    }

    public void Show()
    {
        _mainWindow.Show();

        if (_childWindow.Content == null) return;

        _childWindow.Owner = _mainWindow;
        _childWindow.Top = _mainWindow.Top;
        _childWindow.Left = _mainWindow.Left + _mainWindow.Width;

        var binding = new Binding
        {
            Path = new PropertyPath("ActualHeight"),
            Source = _mainWindow,
            Mode = BindingMode.OneWay
        };
        _childWindow.SetBinding(FrameworkElement.MaxHeightProperty, binding);
        _childWindow.SetBinding(FrameworkElement.MinHeightProperty, binding);

        var multiBinding = new MultiBinding
        {
            Converter = new MyConvertor()
        };

        multiBinding.Bindings.Add(new Binding("ActualWidth") { Source = _mainWindow });
        multiBinding.Bindings.Add(new Binding("Left") { Source = _mainWindow });
        _childWindow.SetBinding(FrameworkElement.MaxWidthProperty, multiBinding);

        _childWindow.SizeToContent = SizeToContent.Width;
        _childWindow.Show();
        _childWindow.SizeToContent = SizeToContent.Manual;
    }

    public static MessageBoxResult ShowDialog(string messageBoxText, MessageBoxButton button = MessageBoxButton.YesNo)
    {
        var caption = Assembly.GetExecutingAssembly().GetName().Name;
        return MessageBox.Show(messageBoxText, caption, button);
    }

    public void ShowProgressBar(ProgressPageViewModel progressPageViewModel)
    {
        SetContent(new ProgressPage(progressPageViewModel));
        _mainWindow.Show();
    }
}

public class MyConvertor : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] is double v1 && values[1] is double v2)
        {
            var maxWidth = SystemParameters.WorkArea.Width - v1 - v2;
            return maxWidth;
        }

        return SystemParameters.WorkArea.Width;
    }


    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}