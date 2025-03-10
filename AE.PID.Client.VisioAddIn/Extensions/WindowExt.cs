using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using ReactiveUI;

namespace AE.PID.Client.VisioAddIn;

public static class WindowExt
{
    public static Task<TResult> ShowDialog<TResult>(this Window dialogWindow)
    {
        var result = new TaskCompletionSource<TResult>();
        var visioHandle = new IntPtr(Globals.ThisAddIn.Application.WindowHandle32);

        RxApp.MainThreadScheduler.Schedule(async void () =>
        {
            try
            {
                if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                    return;

                // create a host window if not exist
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
                };

                // 显示dialog
                var dialogResult = await dialogWindow.ShowDialog<TResult>(desktop.MainWindow);
                result.SetResult(dialogResult);
            }
            catch (Exception e)
            {
                // todo: need to handle exception to avoid crash
                Debugger.Break();
            }
        });
        return result.Task;
    }
}