using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Windows.Forms;
using AE.PID.Controllers.Services;
using AE.PID.Interfaces;
using AE.PID.Models;
using NLog;
using Path = System.IO.Path;

namespace AE.PID;

/// <summary>
///     Main controller
/// </summary>
public partial class ThisAddIn
{
    public readonly string DataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AE\\PID");

    private BackgroundTaskService _backgroundService;
    private Configuration _config;
    private HttpClient _httpClient;
    public IBackgroundTaskService Service => _backgroundService;
    public SelectService SelectService { get; } = new();
    public ExportService ExportService { get; private set; }

    public HttpClient GetHttpClient()
    {
        return _httpClient;
    }

    public Configuration GetCurrentConfiguration()
    {
        return _config;
    }

    private void ThisAddIn_Startup(object sender, EventArgs e)
    {
        try
        {
            Directory.CreateDirectory(Path.Combine(Globals.ThisAddIn.DataFolder));
            Directory.CreateDirectory(Path.Combine(Globals.ThisAddIn.DataFolder, "Libraries"));

            // try to load nlog config from file, copy from resource if not exist
            NLogConfiguration.CreateIfNotExist();
            NLogConfiguration.Load();

            _config = Configuration.Load();
            _httpClient = new HttpClient();

            LogManager.GetCurrentClassLogger().Info($"Staring {Assembly.GetExecutingAssembly().GetName().Name}...");

            _backgroundService = new BackgroundTaskService(LogManager.GetCurrentClassLogger(), _config, _httpClient);
            _backgroundService.Start();

            ExportService = new ExportService(_config);

            LogManager.GetCurrentClassLogger().Info($"{Assembly.GetExecutingAssembly().GetName().Name} Started.");
        }
        catch (Exception ex)
        {
            LogManager.GetCurrentClassLogger().Error(ex.Message);
            MessageBox.Show("Error occured when loading AE PID");
        }
    }

    private void ThisAddIn_Shutdown(object sender, EventArgs e)
    {
        _backgroundService?.Stop();
        Configuration.Save(_config);
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