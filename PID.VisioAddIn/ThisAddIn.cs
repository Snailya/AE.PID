using System;
using System.IO;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Threading;
using AE.PID.Services;
using AE.PID.Tools;
using AE.PID.Views.Windows;
using Microsoft.Office.Interop.Visio;
using Splat;
using Splat.NLog;

namespace AE.PID;

public partial class ThisAddIn : IEnableLogger
{
    private Ribbon _ribbon;

    public static Dispatcher? Dispatcher { get; private set; }

    private void ThisAddIn_Startup(object sender, System.EventArgs e)
    {
        // setup dispatcher
        Dispatcher = Dispatcher.CurrentDispatcher;

        // initialize the data folder
        Directory.CreateDirectory(Constants.LibraryFolder);
        Directory.CreateDirectory(Constants.TmpFolder);

        ConfigureServices();

        // declare a UI thread to display wpf window
        var uiThread = new Thread(WindowManager.Initialize) { Name = "UI Thread" };
        uiThread.SetApartmentState(ApartmentState.STA);
        uiThread.Start();

        WindowManager.Initialized
            .Where(x => x)
            .Subscribe(_ =>
            {
                // initialize background tasks
                BackgroundTaskManager.Initialize();

                // initialize ribbon
                _ribbon = new Ribbon();
                Globals.ThisAddIn.Application.RegisterRibbonX(_ribbon, null,
                    VisRibbonXModes.visRXModeDrawing,
                    "AE PID RIBBON");
            });
    }

    private static void ConfigureServices()
    {
        // register logger
        Locator.CurrentMutable.UseNLogWithWrappingFullLogger();

        // register other services
        Locator.CurrentMutable.RegisterLazySingleton(() => new ConfigurationService(),
            typeof(ConfigurationService));
        Locator.CurrentMutable.RegisterLazySingleton(
            () => new HttpClient { BaseAddress = Locator.Current.GetService<ConfigurationService>()!.Api },
            typeof(HttpClient));
        Locator.CurrentMutable.RegisterLazySingleton(
            () => new MaterialsService(Locator.Current.GetService<HttpClient>()!),
            typeof(MaterialsService));
    }

    private void ThisAddIn_Shutdown(object sender, System.EventArgs e)
    {
        Globals.ThisAddIn.Application.UnregisterRibbonX(_ribbon, null);
        WindowManager.GetInstance()?.Dispose();
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