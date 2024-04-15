using System.Net.Http;
using AE.PID.Controllers.Services;

namespace AE.PID.Controllers;

public class ServiceManager
{
    private static ServiceManager? _instance;

    private ServiceManager()
    {
        Configuration = new ConfigurationService();
        Client = new HttpClient { BaseAddress = Configuration.Api };

        MaterialsService = new MaterialsService(Client);
        DocumentMonitor = new DocumentMonitor(Configuration);
        AppUpdater = new AppUpdater(Client, Configuration);
        LibraryUpdater = new LibraryUpdater(Client, Configuration);
    }


    /// <summary>
    ///     HttpClient should generally be used as a singleton within an application, especially in scenarios where you are
    ///     making multiple HTTP requests. Creating and disposing of multiple instances of HttpClient for each request is not
    ///     recommended, as it can lead to problems such as socket exhaustion and DNS resolution issues.
    /// </summary>
    public HttpClient Client { get; }

    public ConfigurationService Configuration { get; }

    public static ServiceManager GetInstance()
    {
        return _instance ??= new ServiceManager();
    }

    #region Background Services

    public MaterialsService MaterialsService { get; private set; }
    public DocumentMonitor DocumentMonitor { get; private set; }
    public AppUpdater AppUpdater { get; private set; }
    public LibraryUpdater LibraryUpdater { get; }

    #endregion
}