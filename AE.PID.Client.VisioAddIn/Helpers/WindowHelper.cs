using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using AE.PID.Client.Infrastructure.VisioExt;
using AE.PID.Client.VisioAddIn.Services;
using AE.PID.UI.Shared;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Office.Interop.Visio;
using ReactiveUI;
using Application = Avalonia.Application;
using Window = Avalonia.Controls.Window;

namespace AE.PID.Client.VisioAddIn;

public static class WindowHelper
{
    private static readonly Dictionary<string, object> Opened = new();

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
                    parent ??= new IntPtr(Globals.ThisAddIn.Application.WindowHandle);
                    User32.SetWindowLongPtr(new HandleRef(Globals.ThisAddIn.Application, mainWindowHandle.Value),
                        User32.GWL_HWNDPARENT,
                        parent.Value); // 注意设置的是Parent，而不是Owner。如果设置成Owner，会将mainwindow变成子窗口，子窗口的尺寸是无法超过Owner的窗口尺寸的。
                }

                mainWindow.Show();

                // record as opened window
                Opened.Add(windowName, mainWindow);
            });
        }
    }

    public static Task<TResult> ShowDialog<TWindow, TViewModel, TResult>()
        where TWindow : Window where TViewModel : ViewModelBase
    {
        var result = new TaskCompletionSource<TResult>();
    
        var scope = ThisAddIn.Services.CreateScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<TViewModel>();
    
        RxApp.MainThreadScheduler.Schedule(async void () =>
        {
            try
            {
                // create a host window if not exist
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var visioHandle = new IntPtr(Globals.ThisAddIn.Application.WindowHandle32);
    
                    if (desktop.MainWindow is null)
                    {
                        var hostWindow = new Window
                        {
                            Width = 0,
                            Height = 0,
                            ExtendClientAreaToDecorationsHint = false,
                            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome,
                            ExtendClientAreaTitleBarHeightHint = 0,
                            SystemDecorations = SystemDecorations.None,
                            IsVisible = false,
                            ShowInTaskbar = false,
                            ClosingBehavior = WindowClosingBehavior.OwnerAndChildWindows,
                            WindowState = WindowState.Normal,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner
                        };
                        desktop.MainWindow = hostWindow;
                    }
    
                    if (desktop.MainWindow.IsVisible == false)
                    {
                        // 首先通过setParent方法设置hostWindow的Parent为Visio
                        var hostWindowHandle = desktop.MainWindow.TryGetPlatformHandle()?.Handle;
                        // 设置Parent必须在Show之后吗？
                        User32.SetParent(hostWindowHandle!.Value, visioHandle);
    
                        desktop.MainWindow.Show();
    
                        // 顺手查看下当前应用的样式
                        var currentStyle = User32.GetWindowLong(hostWindowHandle.Value, User32.GWL_STYLE);
                        var currentStyleInt = currentStyle.ToInt32(); // 安全转换（样式是 32 位数值）
                        var newStyleInt = (currentStyleInt & ~User32.WS_CHILD) | User32.WS_POPUP;
                        var newStyle = new IntPtr(newStyleInt);
    
                        // 然后通过SetWindowLong设置avaloniaWindow的样式
                        User32.SetWindowLongPtr(new HandleRef(desktop.MainWindow, hostWindowHandle.Value),
                            User32.GWL_STYLE,
                            newStyle);
    
                        // 禁用父窗口
                        User32.EnableWindow(visioHandle, false);
                    }
    
                    var dialogWindow = Activator.CreateInstance<TWindow>();
                    dialogWindow.DataContext = viewModel;
    
                    // 2025.02.03: 关闭模态窗口时，因为关闭了当前激活的窗口，Windows会随机将一个窗口激活，而我们期望的是模态窗口的所有者被激活。
                    // 因此，这里在模态窗口激活之前，首先将所有者窗口激活，详情见https://blog.twofei.com/581/
                    dialogWindow.Closing += (_, _) =>
                    {
                        // 恢复父窗口
                        User32.EnableWindow(visioHandle, true);
                    };
    
                    dialogWindow.Closed += (_, _) =>
                    {
                        // 不需要host了，关闭host窗口
                        desktop.MainWindow.Close();
    
                        // Dispose the scope and any services tied to it when the window closes
                        scope.Dispose();
                    };
    
                    // 显示dialog
                    var dialogResult = await dialogWindow.ShowDialog<TResult>(desktop.MainWindow);
                    result.SetResult(dialogResult);
                }
            }
            catch (Exception e)
            {
                // todo: need to handle exception to avoid crash
                Debugger.Break();
            }
        });
        return result.Task;
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
        User32.SetParent(window, parent);
    }
}