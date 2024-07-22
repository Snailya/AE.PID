using System;
using System.Reactive.Disposables;
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

    public static void Initialize()
    {
        var configuration = Locator.Current.GetService<ConfigurationService>()!;
        if (string.IsNullOrEmpty(configuration.Server) || string.IsNullOrWhiteSpace(configuration.UserId))
        {
            var d = WindowManager.Dispatcher!.BeginInvoke(() =>
                WindowManager.GetInstance()!.ShowDialog(new InitialSetupPage()));
            d.Wait();
        }

        _instance ??= new BackgroundTaskManager();

        LogHost.Default.Info("Background task is running.");
    }
}