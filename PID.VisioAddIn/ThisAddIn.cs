using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Threading;
using AE.PID.Services;
using AE.PID.Tools;
using Microsoft.Office.Interop.Visio;
using Splat;
using Splat.NLog;

namespace AE.PID;

public partial class ThisAddIn : IEnableLogger
{
    private Ribbon? _ribbon;

    public static Dispatcher? Dispatcher { get; private set; }

    private void ThisAddIn_Startup(object sender, System.EventArgs e)
    {
        // the dispatcher of the VSTO is used for dispatch Visio related operation to this thread
        // perform Visio related operation on other threads need to marshal data from the thread to this thread which cause long time
        Dispatcher = Dispatcher.CurrentDispatcher;

        // initialize the data folder
        Directory.CreateDirectory(Constants.LibraryFolder);
        Directory.CreateDirectory(Constants.TmpFolder);

        ConfigureServices();

        // declare a new UI thread for WPF. Notice the apartment state needs to be STA
        var uiThread = new Thread(WindowManager.Initialize) { Name = "UI Thread" };
        uiThread.SetApartmentState(ApartmentState.STA);
        uiThread.Start();

        // initialize the service that relay on the window manager
        WindowManager.Initialized
            .Where(x => x)
            .ObserveOn(ThreadPoolScheduler.Instance)
            .Subscribe(_ =>
            {
                // todo: prompt the configuration if not add
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

        Locator.CurrentMutable.RegisterLazySingleton(() => new ConfigurationService(),
            typeof(ConfigurationService));
        Locator.CurrentMutable.RegisterLazySingleton(() => new ApiClient(), typeof(ApiClient));
        Locator.CurrentMutable.RegisterLazySingleton(() => new MaterialsService(), typeof(MaterialsService));
        Locator.CurrentMutable.RegisterLazySingleton(() => new AppUpdater(), typeof(AppUpdater));
        Locator.CurrentMutable.RegisterLazySingleton(() => new LibraryUpdater(), typeof(LibraryUpdater));
        Locator.CurrentMutable.RegisterLazySingleton(() => new DocumentMonitor(), typeof(DocumentMonitor));
        Locator.CurrentMutable.RegisterLazySingleton(() => new ProjectService(), typeof(ProjectService));
        Locator.CurrentMutable.RegisterLazySingleton(() => new SelectService(), typeof(SelectService));
        
    }

    private void ThisAddIn_Shutdown(object sender, System.EventArgs e)
    {
        // if there is a custom ribbon registered, remove it
        if (_ribbon != null)
            Globals.ThisAddIn.Application.UnregisterRibbonX(_ribbon, null);

        WindowManager.GetInstance()?.Dispose();
        BackgroundTaskManager.GetInstance()?.Dispose();
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