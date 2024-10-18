using System;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Threading;
using AE.PID.Properties;
using AE.PID.ViewModels;
using AE.PID.Views;
using AE.PID.Views.Windows;
using AE.PID.Visio.Core;
using AE.PID.Visio.Core.Dtos;
using MessageBox = System.Windows.MessageBox;

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
        var visioHandle = new IntPtr(Globals.ThisAddIn.Application.WindowHandle32);

        // initialize the main window with normal button
        _mainWindow = new WindowBase(visioHandle)
        {
            WindowButtonStyle = WindowBase.WindowButton.Normal
        };

        _secondaryWindow = new SecondaryWindow(_mainWindow);

        _progressWindow = new WindowBase(visioHandle)
        {
            ShowInTaskbar = false,
            WindowButtonStyle = WindowBase.WindowButton.CloseOnly,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SizeToContent = SizeToContent.WidthAndHeight
        };
    }

    #endregion


    public void Dispose()
    {
        App.UIScheduler.Schedule(() =>
        {
            _secondaryWindow.Close();
            _mainWindow.Close();
            _progressWindow.Close();

            Dispatcher.CurrentDispatcher.InvokeShutdown();
        });
    }


    public static WindowManager? GetInstance()
    {
        return _instance;
    }

    public static void Initialize()
    {
        _instance = new WindowManager();

        // notify the window manager
        // has been initialized so that other tasks based on this manager should start initializing.
        Initialized.OnNext(true);

        // start event loop 
        Dispatcher.Run();
    }

    /// <summary>
    ///     Display a single window.
    /// </summary>
    /// <param name="main"></param>
    /// <typeparam name="TMain"></typeparam>
    public void Show<TMain>(PageBase<TMain> main)
        where TMain : ViewModelBase
    {
        _mainWindow.Content = main;
        _secondaryWindow.Content = null;

        _mainWindow.Show();
    }

    /// <summary>
    ///     Display a window along with a secondary window.
    /// </summary>
    /// <param name="main"></param>
    /// <param name="secondary"></param>
    /// <typeparam name="TMain"></typeparam>
    /// <typeparam name="TSecondary"></typeparam>
    public void Show<TMain, TSecondary>(PageBase<TMain> main, PageBase<TSecondary> secondary)
        where TMain : ViewModelBase where TSecondary : ViewModelBase
    {
        Show(main);

        _secondaryWindow.Content = secondary;
        _secondaryWindow.Show();
    }

    public bool? ShowDialog<TMain>(PageBase<TMain> main) where TMain : ViewModelBase
    {
        _mainWindow.Content = main;
        _secondaryWindow.Content = null;

        return _mainWindow.ShowDialog();
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
    public void CreateRunInBackgroundWithProgress(Progress<ProgressValueDto> progress, Action task)
    {
        App.UIScheduler.Schedule(() =>
        {
            _progressWindow.Content = new ProgressPage(new ProgressPageViewModel(progress, task));
            _progressWindow.Show();
        });
    }
}