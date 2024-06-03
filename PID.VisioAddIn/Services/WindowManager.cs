using System;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using AE.PID.Converters;
using AE.PID.Properties;
using AE.PID.ViewModels;
using AE.PID.Views;
using AE.PID.Views.Windows;
using ReactiveUI;

namespace AE.PID.Services;

public class WindowManager : IDisposable
{
    private static WindowManager? _instance;
    public static readonly BehaviorSubject<bool> Initialized = new(false);
    private readonly ChildWindow _childWindow = new();
    private readonly MainWindow _mainWindow = new();

    private WindowManager()
    {
        _mainWindow.Title = Resources.PROPERTY_product_name;

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
        RxApp.MainThreadScheduler = DispatcherScheduler.Current;

        // notify initialized
        Initialized.OnNext(true);
        // start event loop
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
        _mainWindow.CenterOwner();

        if (_childWindow.Content == null) return;

        // if there's a child content
        _childWindow.Owner = _mainWindow;
        _childWindow.Top = _mainWindow.Top;
        _childWindow.Left = _mainWindow.Left + _mainWindow.Width;

        var heightBinding = new Binding
        {
            Path = new PropertyPath("ActualHeight"),
            Source = _mainWindow,
            Mode = BindingMode.OneWay
        };
        _childWindow.SetBinding(FrameworkElement.MaxHeightProperty, heightBinding);
        _childWindow.SetBinding(FrameworkElement.MinHeightProperty, heightBinding);

        var maxWidthBinding = new MultiBinding
        {
            Converter = new SideWindowMaxWidthConvertor()
        };

        maxWidthBinding.Bindings.Add(new Binding("ActualWidth") { Source = _mainWindow });
        maxWidthBinding.Bindings.Add(new Binding("Left") { Source = _mainWindow });
        _childWindow.SetBinding(FrameworkElement.MaxWidthProperty, maxWidthBinding);

        _childWindow.Show();
    }

    /// <summary>
    ///     Show a modal dialog to alert user or request confirmation.
    /// </summary>
    /// <param name="messageBoxText"></param>
    /// <param name="button"></param>
    /// <returns></returns>
    public static MessageBoxResult ShowDialog(string messageBoxText, MessageBoxButton button = MessageBoxButton.YesNo)
    {
        return MessageBox.Show(messageBoxText, Resources.PROPERTY_product_name, button);
    }
    
    /// <summary>
    ///     Show a progress bar to provide better user experience.
    /// </summary>
    public void CreateRunInBackgroundWithProgress(Progress<ProgressValue> progress, Action task)
    {
        Dispatcher!.Invoke(() =>
        {
            SetContent(new ProgressPage(new ProgressPageViewModel(progress, task)));
            _mainWindow.Show();
        });
    }
}