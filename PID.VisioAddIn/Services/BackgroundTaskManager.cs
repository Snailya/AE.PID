using System.Net.Http;
using Splat;

namespace AE.PID.Services;

public class BackgroundTaskManager
{
    private static BackgroundTaskManager? _instance;

    public AppUpdater AppUpdater { get; set; } = new(Locator.Current.GetService<HttpClient>()!,
        Locator.Current.GetService<ConfigurationService>()!);

    public LibraryUpdater LibraryUpdater { get; set; } = new(Locator.Current.GetService<HttpClient>()!,
        Locator.Current.GetService<ConfigurationService>()!);

    public DocumentMonitor DocumentMonitor { get; set; } = new(Locator.Current.GetService<ConfigurationService>()!);

    public static BackgroundTaskManager? GetInstance()
    {
        return _instance;
    }

    public static void Initialize()
    {
        _instance ??= new BackgroundTaskManager();

        LogHost.Default.Info("Background task is running.");
    }
}