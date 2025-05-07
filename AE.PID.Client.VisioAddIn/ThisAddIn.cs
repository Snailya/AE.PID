using System;
using System.Reactive.Concurrency;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;
using AE.PID.Client.Core;
using AE.PID.Client.Infrastructure;
using AE.PID.Client.Infrastructure.VisioExt;
using AE.PID.Client.UI.Avalonia;
using Microsoft.Office.Core;
using Splat;
using VisioApp = Microsoft.Office.Interop.Visio.Application;

namespace AE.PID.Client.VisioAddIn;

public partial class ThisAddIn : IEnableLogger
{
    private Bootstrapper _bootstrapper;
    public static IServiceBridge ServiceBridge { get; private set; } = null!;

    /// <summary>
    ///     Get the handle of the visio application
    /// </summary>
    /// <returns></returns>
    public static IntPtr GetApplicationHandle()
    {
        return new IntPtr(Globals.ThisAddIn.Application.WindowHandle32);
    }

    /// <summary>
    ///     Execute COM operation
    /// </summary>
    /// <param name="action"></param>
    public static void SafeExecute(Action<VisioApp> action)
    {
        try
        {
            action.Invoke(Globals.ThisAddIn.Application);
        }
        catch (COMException comEx)
        {
            LogHost.Default.Error(comEx.Message);
        }
        catch (Exception ex)
        {
        }
    }

    private async void ThisAddIn_Startup(object sender, EventArgs e)
    {
        // initialize bootstrapper, the bootstrapper is used for initialize logger, ui thread and host
        _bootstrapper = new Bootstrapper();
        ServiceBridge = _bootstrapper;

        // initialize a scheduler so that we could schedule visio related work on this thread,
        // because the main thread has no synchronization context, a new synchronization context is created
        var mainContext = SynchronizationContext.Current ?? new SynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(mainContext);

        // initialize scheduler manager
        SchedulerManager.VisioScheduler = new DispatcherScheduler(Dispatcher.CurrentDispatcher);

        try
        {
            // 等待初始化完成后执行
            await _bootstrapper.WaitForReadyAsync(TimeSpan.FromSeconds(30));

            var configuration = ServiceBridge.GetRequiredService<IConfigurationService>();
            PromptUserIdInput(configuration);
            CheckUpdate(configuration);
        }
        catch (Exception ex)
        {
            MessageBox.Show($@"AE PID加载失败: {ex.Message}");
            Dispose();
        }
    }

    private static void CheckUpdate(IConfigurationService configuration)
    {
        // 检查更新
        var checker = ServiceBridge.GetRequiredService<UpdateChecker>();
        // 2025.04.11: 增加给更新通道选择
        var currentConfiguration = configuration.GetCurrentConfiguration();
        _ = checker.CheckAsync(
            configuration.RuntimeConfiguration.Version,
            currentConfiguration.Server + $"/api/v3/app?channel={(int)currentConfiguration.Channel}");
    }

    private static void PromptUserIdInput(IConfigurationService configuration)
    {
        // 如果当前的用户ID是空值，提示用户输入UserId
        var ui = ServiceBridge.GetRequiredService<IUserInteractionService>();
        var vm = ServiceBridge.GetRequiredService<SettingsWindowViewModel>();
        if (string.IsNullOrEmpty(configuration.GetCurrentConfiguration().UserId))
            ui.Show(vm, GetApplicationHandle());
    }


    private async void ThisAddIn_Shutdown(object sender, EventArgs e)
    {
        await _bootstrapper.ShutdownAsync();
        _bootstrapper.Dispose();
    }

    protected override IRibbonExtensibility CreateRibbonExtensibilityObject()
    {
        return new Ribbon();
    }


    #region VSTO generated code

    /// <summary>
    ///     Required method for Designer support - do not modify
    ///     the contents of this method with the code editor.
    /// </summary>
    private void InternalStartup()
    {
        Startup += ThisAddIn_Startup;
        Shutdown += ThisAddIn_Shutdown;
    }

    #endregion
}