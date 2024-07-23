using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using AE.PID.Views;
using Splat;

namespace AE.PID.Services;

public class BackgroundTaskManager : IDisposable
{
    private static BackgroundTaskManager? _instance;
    private readonly CompositeDisposable _cleanUp = new();

    public void Dispose()
    {
        _cleanUp.Dispose();
    }

    public static BackgroundTaskManager? GetInstance()
    {
        return _instance;
    }

    public static async Task Initialize()
    {
        var configuration = Locator.Current.GetService<ConfigurationService>()!;

        if (string.IsNullOrEmpty(configuration.Server) || string.IsNullOrWhiteSpace(configuration.UserId))
            await Observable.Start(() => WindowManager.GetInstance()!.ShowDialog(new InitialSetupPage()),
                AppScheduler.UIScheduler).ToTask();

        _instance ??= new BackgroundTaskManager();

        LogHost.Default.Info("Background task is running.");
    }
}