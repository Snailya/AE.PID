using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using AE.PID.Views;
using AE.PID.Views.Pages;
using NLog;

namespace AE.PID.Controllers.Services;

/// <summary>
/// This class handles the invoking of showing a configuration setup view from ribbon.
/// </summary>
public abstract class ConfigurationUpdater
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static Subject<Unit> ManuallyInvokeTrigger { get; } = new();

    /// <summary>
    ///     Trigger manually.
    /// </summary>
    public static void Invoke()
    {
        ManuallyInvokeTrigger.OnNext(Unit.Default);
    }

    /// <summary>
    /// Start listening for user setting button click event and display a view to accept user operation.
    /// The view prompt user to modify key values in configuration file and invoke update manually, and the subsequent is called in ViewModel. 
    /// </summary>
    /// <returns></returns>
    public static IDisposable Listen()
    {
        Logger.Info($"Configuration Update Service started.");

        return ManuallyInvokeTrigger
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(Globals.ThisAddIn.SynchronizationContext)
            .Subscribe(
                _ =>
                {
                    try
                    {
                        Globals.ThisAddIn.WindowManager.Show(new UserSettingsPage());
                    }
                    catch (Exception ex)
                    {
                        ThisAddIn.Alert($"加载失败：{ex.Message}");
                        Logger.Error(ex,
                            $"Failed to display setting window.");
                    }
                },
                ex =>
                {
                    Logger.Error(ex,
                        $"Configuration Update Service ternimated accidently.");
                },
                () => { Logger.Error("Configuration Update Service should never complete."); });
    }
}