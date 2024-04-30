using System;
using System.IO;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using AE.PID.Controllers.Services;
using AE.PID.Models;
using AE.PID.Properties;
using AE.PID.ViewModels;
using AE.PID.ViewModels.Pages;
using AE.PID.Views;
using AE.PID.Views.Windows;
using Microsoft.Office.Interop.Visio;
using NLog;
using Splat;
using MessageBox = System.Windows.Forms.MessageBox;
using Window = System.Windows.Window;

namespace AE.PID;

public partial class ThisAddIn
{
    private readonly CompositeDisposable _compositeDisposable = new();
    private Logger _logger;
    private Ribbon _ribbon;

    /// <summary>
    ///     The synchronization context for wpf dispatching.
    /// </summary>
    public SynchronizationContext SynchronizationContext;

    /// <summary>
    ///     Manges window and side window, make all windows reusable to reduce memory usage.
    /// </summary>
    public WindowManager WindowManager;


    /// <summary>
    ///     The user input cache of the previous input.
    /// </summary>
    public InputCache InputCache { get; private set; }


    public static DialogResult AskForUpdate(string description, string caption = "")
    {
        if (string.IsNullOrEmpty(caption))
            caption = Resources.Product_name;

        return MessageBox.Show(description, caption, MessageBoxButtons.YesNo);
    }

    public static void Alert(string description, string caption = "")
    {
        if (string.IsNullOrEmpty(caption))
            caption = Resources.Product_name;

        MessageBox.Show(description, caption);
    }

    public void ShowProgressWhileActing(Action<IProgress<int>, CancellationToken> action)
    {
        // initialize a cts as it's a long time consumed task that user might not want to wait until it finished.
        var cts = new CancellationTokenSource();
        var vm = new TaskProgressViewModel(cts);

        // create a window center Visio App
        var window = new Window { Content = new TaskProgressView(vm) };

        // do updates in a background thread thought VISIO is STA, but still could do in a single thread without concurrent
        Observable.Create<int>(async observer =>
            {
                try
                {
                    var progress = new Progress<int>(observer.OnNext);
                    await Task.Run(() => action(progress, cts.Token), cts.Token);
                    observer.OnCompleted();
                }
                catch (OperationCanceledException)
                {
                    _logger.Info("Process cancelled by user.");
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }

                // if the subscription to this observable is disposed, the task should also be canceled as it is not monitor by anyone
                return () => cts.Cancel();
            })
            .ObserveOn(window.Dispatcher)
            .Subscribe(
                value => { vm.Current = value; },
                ex =>
                {
                    window.Visibility = Visibility.Collapsed;

                    _logger.Error(ex,
                        "Process failed.");
                    Alert(ex.Message);
                },
                () => { });

        // show a progress bar while executing this long running task
        window.Show();
    }

    private void ThisAddIn_Startup(object sender, EventArgs e)
    {
        _ribbon = new Ribbon();
        Globals.ThisAddIn.Application.RegisterRibbonX(_ribbon, null,
            VisRibbonXModes.visRXModeDrawing,
            "AE PID RIBBON");

        // associate the main tread with a synchronization context so that the main thread would be achieved using SynchronizationContext.Current.
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
        SynchronizationContext = SynchronizationContext.Current;

        WindowManager = new WindowManager();

        Setup();
    }

    private void ThisAddIn_Shutdown(object sender, EventArgs e)
    {
        Globals.ThisAddIn.Application.UnregisterRibbonX(_ribbon, null);

        InputCache.Save(InputCache);

        _compositeDisposable.Dispose();
    }

    /// <summary>
    ///     Initialize the configuration and environment setup.
    /// </summary>
    private void Setup()
    {
        try
        {
            // initialize the data folder
            Directory.CreateDirectory(Constants.LibraryFolder);
            Directory.CreateDirectory(Constants.TmpFolder);

            // initialize logger
            _logger = LogManager.GetCurrentClassLogger();

            // load the input cache
            InputCache = InputCache.Load();


            Locator.CurrentMutable.RegisterLazySingleton(() => new ConfigurationService(),
                typeof(ConfigurationService));

            var configuration = Locator.Current.GetService<ConfigurationService>();
            Locator.CurrentMutable.RegisterLazySingleton(() => new HttpClient { BaseAddress = configuration!.Api },
                typeof(HttpClient));

            var httpclient = Locator.Current.GetService<HttpClient>();
            Locator.CurrentMutable.RegisterLazySingleton(() => new MaterialsService(httpclient!),
                typeof(MaterialsService));
            Locator.CurrentMutable.RegisterLazySingleton(() => new DocumentMonitor(configuration!),
                typeof(DocumentMonitor));
            Locator.CurrentMutable.RegisterLazySingleton(() => new AppUpdater(httpclient!, configuration!),
                typeof(AppUpdater));
            Locator.CurrentMutable.RegisterLazySingleton(() => new LibraryUpdater(httpclient!, configuration!),
                typeof(LibraryUpdater));
        }
        catch (UnauthorizedAccessException unauthorizedAccessException)
        {
            _logger.Error(unauthorizedAccessException, "Failed to create directory due to authority issue on setup.");
        }
        catch (PathTooLongException pathTooLongException)
        {
            _logger.Error(pathTooLongException,
                "Failed to create directory on setup because the folder path is too long.");
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to setup.");
        }

        _logger.Info("Setuped.");
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