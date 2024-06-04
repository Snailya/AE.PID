using System;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Threading;
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

    private readonly WindowBase _mainWindow;
    private readonly WindowBase _progressWindow;
    private readonly SecondaryWindow _secondaryWindow;

    #region Constructors

    private WindowManager()
    {
        _mainWindow = new WindowBase(new IntPtr(Globals.ThisAddIn.Application.WindowHandle32))
        {
            WindowButtonStyle = WindowBase.WindowButton.Normal
        };

        _secondaryWindow = new SecondaryWindow(_mainWindow);

        _progressWindow = new WindowBase(new IntPtr(Globals.ThisAddIn.Application.WindowHandle32))
        {
            ShowInTaskbar = false,
            WindowButtonStyle = WindowBase.WindowButton.CloseOnly
        };
    }

    #endregion

    public static Dispatcher? Dispatcher { get; private set; }

    public void Dispose()
    {
        Dispatcher?.Invoke(() =>
        {
            _secondaryWindow.Close();
            _mainWindow.Close();
            _progressWindow.Close();
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
        _secondaryWindow.Content = child;
    }

    public void SetContent<TMain>(PageBase<TMain> main)
        where TMain : ViewModelBase
    {
        _mainWindow.Content = main;
        _secondaryWindow.Content = null;
    }

    public void Show()
    {
        _mainWindow.Show();
        _mainWindow.CenterOwner();

        if (_secondaryWindow.Content == null) return;
        _secondaryWindow.Show();
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
            _progressWindow.Content = new ProgressPage(new ProgressPageViewModel(progress, task));
            _progressWindow.Show();
        });
    }
}