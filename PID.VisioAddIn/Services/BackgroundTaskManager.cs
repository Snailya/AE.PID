using System;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using Splat;

namespace AE.PID.Services;

public class BackgroundTaskManager : IDisposable
{
    private static BackgroundTaskManager? _instance;
    private readonly CompositeDisposable _cleanUp = new();

    private BackgroundTaskManager(HttpClient httpClient, ConfigurationService configuration)
    {
        AppUpdater = new AppUpdater(httpClient, configuration);
        LibraryUpdater = new LibraryUpdater(httpClient, configuration);
        DocumentMonitor = new DocumentMonitor(configuration);
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
        var httpClient = Locator.Current.GetService<HttpClient>()!;
        var configuration = Locator.Current.GetService<ConfigurationService>()!;

        _instance ??= new BackgroundTaskManager(httpClient, configuration);
        
        LogHost.Default.Info("Background task is running.");
    }
}