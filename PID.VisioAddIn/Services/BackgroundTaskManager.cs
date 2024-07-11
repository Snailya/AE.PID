using System;
using System.Reactive.Disposables;
using Splat;

namespace AE.PID.Services;

public class BackgroundTaskManager : IDisposable
{
    private static BackgroundTaskManager? _instance;
    private readonly CompositeDisposable _cleanUp = new();

    private BackgroundTaskManager(ApiClient client, ConfigurationService configuration)
    {
        AppUpdater = new AppUpdater(client, configuration);
        LibraryUpdater = new LibraryUpdater(client, configuration);
        DocumentMonitor = new DocumentMonitor(client, configuration);
    }

    public AppUpdater AppUpdater { get; set; }

    public LibraryUpdater LibraryUpdater { get; set; }

    public DocumentMonitor DocumentMonitor { get; set; }

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
        var client = Locator.Current.GetService<ApiClient>()!;
        var configuration = Locator.Current.GetService<ConfigurationService>()!;

        _instance ??= new BackgroundTaskManager(client, configuration);

        LogHost.Default.Info("Background task is running.");
    }
}