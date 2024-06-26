﻿using System;
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

        // initialize background tasks
        WindowManager.Initialized
            .Where(x => x)
            .ObserveOn(ThreadPoolScheduler.Instance)
            .Subscribe(_ =>
            {
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
        Locator.CurrentMutable.RegisterLazySingleton(
            () => new ApiClient(Locator.Current.GetService<ConfigurationService>()!),
            typeof(ApiClient));
        Locator.CurrentMutable.RegisterLazySingleton(
            () => new MaterialsService(Locator.Current.GetService<ApiClient>()!), typeof(MaterialsService));
    }

    private void ThisAddIn_Shutdown(object sender, System.EventArgs e)
    {
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