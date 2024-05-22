using System.Net.Http;
using Splat;

namespace AE.PID.Services;

public class BackgroundManager
{
    private static BackgroundManager? _instance;

    public AppUpdater AppUpdater { get; set; } = new(Locator.Current.GetService<HttpClient>()!,
        Locator.Current.GetService<ConfigurationService>()!);

    public LibraryUpdater LibraryUpdater { get; set; } = new(Locator.Current.GetService<HttpClient>()!,
        Locator.Current.GetService<ConfigurationService>()!);

    public DocumentMonitor DocumentMonitor { get; set; } = new(Locator.Current.GetService<ConfigurationService>()!);

    public static BackgroundManager? GetInstance()
    {
        return _instance ??= new BackgroundManager();
    }
}