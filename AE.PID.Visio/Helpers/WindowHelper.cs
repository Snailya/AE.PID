using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using AE.PID.Visio.Services;
using AE.PID.Visio.Shared;
using AE.PID.Visio.UI.Avalonia.ViewModels;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Office.Interop.Visio;
using ReactiveUI;
using Window = Avalonia.Controls.Window;

namespace AE.PID.Visio.Helpers;

public static class WindowHelper
{
    private const int GWL_HWNDPARENT = -8;
    private static readonly Dictionary<string, object> Opened = new();

    [DllImport("user32.dll")]
    private static extern bool SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy,
        uint uFlags);

    public static void Show<TWindow, TViewModel>(IntPtr? parent = null)
        where TWindow : Window where TViewModel : ViewModelBase
    {
        var windowName = typeof(TWindow).Name;

        if (Opened.TryGetValue(windowName, out var window) && window is TWindow)
        {
            // if there is already an opened window with the same id, no need to re-create it.
        }
        else
        {
            var scope = ThisAddIn.Services.CreateScope();
            var viewModel = scope.ServiceProvider.GetRequiredService<TViewModel>();

            RxApp.MainThreadScheduler.Schedule(() =>
            {
                var mainWindow = Activator.CreateInstance<TWindow>();
                mainWindow.DataContext = viewModel;

                mainWindow.Closed += (_, _) =>
                {
                    // remove the window from the opened window list
                    Opened.Remove(windowName);

                    // Dispose the scope and any services tied to it when the window closes
                    scope.Dispose();
                };

                // set the window's parent as the Visio Application
                var mainWindowHandle = mainWindow.TryGetPlatformHandle()?.Handle;
                if (mainWindowHandle.HasValue)
                {
                    parent ??= new IntPtr(Globals.ThisAddIn.Application.WindowHandle32);
                    SetWindowLong(mainWindowHandle.Value, GWL_HWNDPARENT,
                        parent.Value); // 注意设置的是Parent，而不是Owner。如果设置成Owner，会将mainwindow变成子窗口，子窗口的尺寸是无法超过Owner的窗口尺寸的。
                }

                mainWindow.Show();

                // record as opened window
                Opened.Add(windowName, mainWindow);
            });
        }
    }

    public static void ShowTaskPane<TView, TViewModel>(string caption,
        Action<Shape, TViewModel> onSelectionChanged)
        where TView : UserControl where TViewModel : ViewModelBase
    {
        var viewName = typeof(TView).Name;

        if (Opened.TryGetValue(viewName, out var control) && control is TView)
        {
            // if there is already an opened window with the same id, no need to re-create it.
        }
        else
        {
            var scope = ThisAddIn.Services.CreateScope();

            Observable.Start(() =>
                {
                    var viewModel = scope.ServiceProvider.GetRequiredService<TViewModel>();
                    // update the viewmodel when selection change
                    var subscription = Observable
                        .FromEvent<EWindow_SelectionChangedEventHandler, Microsoft.Office.Interop.Visio.Window>(
                            handler => Globals.ThisAddIn.Application.ActiveWindow.SelectionChanged += handler,
                            handler => Globals.ThisAddIn.Application.ActiveWindow.SelectionChanged -= handler,
                            SchedulerManager.VisioScheduler
                        )
                        .Where(x => x.Selection.Count == 1 && x.Selection[1] is { } shape)
                        .Select(x => x.Selection[1])
                        .Subscribe(shape => onSelectionChanged(shape, viewModel)); // todo:不知道为什么第一次打开窗口的时候，总是无法触发这个订阅

                    var view = Activator.CreateInstance<TView>();
                    view.DataContext = viewModel;

                    var pane = new VisioTaskPane(view);
                    pane.HandleDestroyed += (_, _) =>
                    {
                        // remove the window from the opened window list
                        Opened.Remove(viewName);

                        // dispose the selection change event handler as the pane is no longer visible
                        subscription.Dispose();

                        // Dispose the scope and any services tied to it when the window closes
                        scope.Dispose();
                    };

                    return new { pane, viewModel };
                }, RxApp.MainThreadScheduler)
                .ObserveOn(SchedulerManager.VisioScheduler)
                .Select(v =>
                {
                    // set up the initial value
                    onSelectionChanged(Globals.ThisAddIn.Application.ActiveWindow.Selection[1], v.viewModel);

                    var hostWindow = Globals.ThisAddIn.Application.ActiveWindow.Windows.Add(caption,
                        VisWindowStates.visWSVisible | VisWindowStates.visWSAnchorRight |
                        VisWindowStates.visWSAnchorTop,
                        VisWinTypes.visAnchorBarAddon,
                        0, 0,
                        v.pane.Width, v.pane.Height);

                    return new { v.pane, hostWindow };
                })
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(v =>
                {
                    SetOwner(v.pane.Handle, new IntPtr(v.hostWindow.WindowHandle32));

                    // record as opened window
                    Opened.Add(viewName, v.pane);
                });
        }
    }

    private static void SetOwner(IntPtr window, IntPtr parent)
    {
        SetParent(window, parent);
    }
}