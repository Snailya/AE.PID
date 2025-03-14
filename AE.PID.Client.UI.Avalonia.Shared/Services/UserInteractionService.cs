using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Runtime.InteropServices;
using AE.PID.Client.Core;
using AE.PID.Client.UI.Avalonia.VisioExt;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using ReactiveUI;
using Splat;

namespace AE.PID.Client.UI.Avalonia.Shared;

public class UserInteractionService : IUserInteractionService, IEnableLogger
{
    private static readonly ConcurrentDictionary<string, Window> Opened = new();

    public void Show<TViewModel>(TViewModel vm, IntPtr? parent = null) where TViewModel : INotifyPropertyChanged
    {
        RxApp.MainThreadScheduler.Schedule(() =>
        {
            var mainWindow = Build(vm);
            if (mainWindow == null) throw new Exception("Window not found");

            mainWindow.Closed += (_, _) =>
            {
                // // remove the window from the opened window list
                // Opened.Remove(windowName);

                // Dispose the scope and any services tied to it when the window closes
                if (vm is IDisposable disposable)
                    disposable.Dispose();
            };

            // set the window's parent as the Visio Application
            if (parent != null)
                // windows平台调用user32
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var mainWindowHandle = mainWindow.TryGetPlatformHandle()?.Handle;
                    if (mainWindowHandle.HasValue)
                        User32.SetWindowLongPtr(new HandleRef(mainWindow, mainWindowHandle.Value),
                            User32.GWL_HWNDPARENT,
                            parent.Value); // 注意设置的是Parent，而不是Owner。如果设置成Owner，会将mainwindow变成子窗口，子窗口的尺寸是无法超过Owner的窗口尺寸的。
                }

            mainWindow.Show();

            // // record as an opened window
            // Opened.Add(windowName, mainWindow);
        });
    }

    public Task<TResult> ShowDialog<TViewModel, TResult>(TViewModel vm, IntPtr? parent = null)
        where TViewModel : INotifyPropertyChanged
    {
        var result = new TaskCompletionSource<TResult>();

        RxApp.MainThreadScheduler.Schedule(async void () =>
        {
            var dialogWindow = Build(vm);
            if (dialogWindow == null) throw new Exception("Window not found");

            try
            {
                // create a host window if not exist
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    if (desktop.MainWindow is null)
                    {
                        var hiddenHostWindow = new Window
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
                        desktop.MainWindow = hiddenHostWindow;
                    }

                    if (parent.HasValue)
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            // 首先通过setParent方法设置hostWindow的Parent为Visio
                            var hostWindowHandle = desktop.MainWindow.TryGetPlatformHandle()?.Handle;
                            // 设置Parent必须在Show之后吗？
                            User32.SetParent(hostWindowHandle!.Value, parent.Value);

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
                            User32.EnableWindow(parent.Value, false);

                            // 2025.02.03: 关闭模态窗口时，因为关闭了当前激活的窗口，Windows会随机将一个窗口激活，而我们期望的是模态窗口的所有者被激活。
                            // 因此，这里在模态窗口激活之前，首先将所有者窗口激活，详情见https://blog.twofei.com/581/
                            dialogWindow.Closing += (_, _) =>
                            {
                                // 恢复父窗口
                                User32.EnableWindow(parent.Value, true);
                            };
                        }

                    if (desktop.MainWindow.IsVisible == false) desktop.MainWindow.Show();
                    dialogWindow.Closed += (_, _) =>
                    {
                        // 不需要host了，关闭host窗口
                        desktop.MainWindow.Close();

                        // Dispose the scope and any services tied to it when the window closes
                        if (vm is IDisposable disposable)
                            disposable.Dispose();
                    };

                    // 显示dialog
                    var dialogResult = await dialogWindow.ShowDialog<TResult>(desktop.MainWindow);
                    result.SetResult(dialogResult);
                }
            }
            catch (Exception e)
            {
                this.Log().Error(e, "Error showing dialog.");
            }
        });
        return result.Task;
    }

    public Task<bool> SimpleDialog(string message, string? title)
    {
        var vm = new SimpleDialogViewModel(message, title);
        return ShowDialog<SimpleDialogViewModel, bool>(vm);
    }

    private static Window? Build(object? data)
    {
        if (data is null)
            return null;

        // 首先尝试查找同名的Window
        var name = data.GetType().FullName!.Replace("ViewModel", "");

        // 由于不知道window对应的类型在哪个程序集中定义的，需要加载这个程序集。
        var assembly = Assembly.Load(data.GetType().Assembly.FullName);
        var type = assembly.GetType(name);

        // 如果没有匹配的Window，再尝试查找同名的View
        if (type == null)
        {
            name = data.GetType().FullName!.Replace("ViewModel", "View");
            type = assembly.GetType(name);

            // 如果也没有匹配的View，则显示未找到窗口
            if (type == null)
                return new Window
                {
                    Content = new TextBlock { Text = "Not Found: " + name }
                };

            var view = (Control?)Activator.CreateInstance(type)!;
            view.DataContext = data;
            return new Window
            {
                Content = view
            };
        }

        var window = (Window?)Activator.CreateInstance(type)!;
        window.DataContext = data;
        return window;
    }
}