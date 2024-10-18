using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using AE.PID.Services;
using AE.PID.Views;
using AE.PID.Visio.Core;
using AE.PID.Visio.Infrastructure.Services;
using Microsoft.Office.Interop.Visio;
using ReactiveUI;
using Splat;
using Splat.NLog;

namespace AE.PID;

public partial class ThisAddIn : IEnableLogger
{
    private Ribbon? _ribbon;


    private void ThisAddIn_Startup(object sender, System.EventArgs e)
    {
        // initialize a scheduler so that we could schedule visio related work on this thread,
        // because the main thread has no synchronization context, a new synchronization context is created
        var mainContext = SynchronizationContext.Current ?? new SynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(mainContext);
        ThisAddIn.Scheduler = new SynchronizationContextScheduler(mainContext);

        // declare a new UI thread for WPF. Notice the apartment state needs to be STA
        var uiThread = new Thread(() =>
        {
            // initialize a dispatcher scheduler to schedule ui related work on this thread.
            App.UIScheduler = new DispatcherScheduler(Dispatcher.CurrentDispatcher);
            RxApp.MainThreadScheduler = DispatcherScheduler.Current;

            WindowManager.Initialize();
        }) { Name = "UI Thread" };
        uiThread.SetApartmentState(ApartmentState.STA);
        uiThread.Start();

        // initialize the data folder
        Directory.CreateDirectory(App.LibraryFolder);
        Directory.CreateDirectory(App.TmpFolder);

        ConfigureServices();

        // invoke an initial set up page if necessary
        Task.Run(async () =>
        {
            var configuration = Locator.Current.GetService<IConfigurationService>()!;
            if (string.IsNullOrEmpty(configuration.Server) || string.IsNullOrWhiteSpace(configuration.UserId))
                await Observable.Start(() => WindowManager.GetInstance()!.ShowDialog(new InitialSetupPage()),
                    App.UIScheduler).ToTask();
        });

        // initialize ribbon
        _ribbon = new Ribbon();
        Globals.ThisAddIn.Application.RegisterRibbonX(_ribbon, null,
            VisRibbonXModes.visRXModeDrawing,
            "AE PID RIBBON");
    }

    private static void ConfigureServices()
    {
        // register logger
        Locator.CurrentMutable.UseNLogWithWrappingFullLogger();

        Locator.CurrentMutable.RegisterLazySingleton(() => new ConfigurationService(),
            typeof(IConfigurationService));
        Locator.CurrentMutable.RegisterLazySingleton(() => new MaterialsService(), typeof(IMaterialService));
        Locator.CurrentMutable.RegisterLazySingleton(() => new DocumentMonitor(), typeof(DocumentMonitor));
        Locator.CurrentMutable.RegisterLazySingleton(() => new UiService(), typeof(IUiService));
        Locator.CurrentMutable.RegisterLazySingleton(() => new ApiFactory(), typeof(ApiFactory));
        Locator.CurrentMutable.RegisterLazySingleton(() => new VisioService(), typeof(IVisioService));

        Locator.CurrentMutable.Register(() => new ProjectService(), typeof(IProjectService));

        Locator.CurrentMutable.RegisterConstant(new AppUpdater(), typeof(AppUpdater));
        Locator.CurrentMutable.RegisterConstant(new LibraryUpdater(), typeof(LibraryUpdater));
    }

    private void ThisAddIn_Shutdown(object sender, System.EventArgs e)
    {
        // if there is a custom ribbon registered, remove it
        if (_ribbon != null)
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