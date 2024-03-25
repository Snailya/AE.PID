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
using AE.PID.Controllers;
using AE.PID.Controllers.Services;
using AE.PID.Models;
using AE.PID.Models.Configurations;
using AE.PID.Properties;
using AE.PID.ViewModels;
using AE.PID.Views;
using AE.PID.Views.Windows;
using NLog;
using MessageBox = System.Windows.Forms.MessageBox;
using Path = System.IO.Path;

namespace AE.PID;

public partial class ThisAddIn
{
    private Logger _logger;
    private Ribbon _ribbon;

    private readonly CompositeDisposable _compositeDisposable = new();

    /// <summary>
    ///     The data folder path in Application Data.
    /// </summary>
    public static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AE\\PID");

    /// <summary>
    ///     The tmp folder to store updated file.
    /// </summary>
    public static readonly string LibraryFolder = Path.Combine(AppDataFolder, "Libraries");

    /// <summary>
    ///     The tmp folder to store updated file.
    /// </summary>
    public static readonly string LibraryCheatSheet = Path.Combine(LibraryFolder, ".cheatsheet");
    
    /// <summary>
    ///     The tmp folder to store updated file.
    /// </summary>
    public static readonly string TmpFolder = Path.Combine(AppDataFolder, "Tmp");

    /// <summary>
    /// Manges window and side window, make all windows reusable to reduce memory usage.
    /// </summary>
    public WindowManager WindowManager;

    /// <summary>
    /// Manges all services
    /// </summary>
    public ServiceManager ServiceManager;

    /// <summary>
    /// The synchronization context for wpf dispatching.
    /// </summary>
    public SynchronizationContext SynchronizationContext;

    /// <summary>
    ///     Configuration has three part: app configuration, library configuration, export settings.
    /// </summary>
    public Configuration Configuration { get; private set; }

    /// <summary>
    /// The user input cache of the previous input.
    /// </summary>
    public InputCache InputCache { get; private set; }

    /// <summary>
    ///     HttpClient should generally be used as a singleton within an application, especially in scenarios where you are
    ///     making multiple HTTP requests. Creating and disposing of multiple instances of HttpClient for each request is not
    ///     recommended, as it can lead to problems such as socket exhaustion and DNS resolution issues.
    /// </summary>
    public HttpClient HttpClient { get; } = new();

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
                        $"Process failed.");
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
            Microsoft.Office.Interop.Visio.VisRibbonXModes.visRXModeDrawing,
            "AE PID RIBBON");

        // associate the main tread with a synchronization context so that the main thread would be achieved using SynchronizationContext.Current.
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
        SynchronizationContext = SynchronizationContext.Current;

        WindowManager = new WindowManager();

        // invoke the setup process immediately
        var setupObservable = Observable.Start(Setup);
        setupObservable.Subscribe(_ =>
        {
            HttpClient.BaseAddress = new Uri(Configuration.Api);

            ServiceManager = new ServiceManager(httpClient: HttpClient);

            // background service
            AppUpdater.Listen()
                .DisposeWith(_compositeDisposable);
            LibraryUpdater.Listen()
                .DisposeWith(_compositeDisposable);
            DocumentUpdater.Listen()
                .DisposeWith(_compositeDisposable);
            LinkedControlManager.Listen()
                .DisposeWith(_compositeDisposable);

            // ribbon
            DocumentInitializer.Listen()
                .DisposeWith(_compositeDisposable);
            ShapeSelector.Listen()
                .DisposeWith(_compositeDisposable);
            LegendService.Listen()
                .DisposeWith(_compositeDisposable);

            DocumentExporter.Listen()
                .DisposeWith(_compositeDisposable);

            ConfigurationUpdater.Listen()
                .DisposeWith(_compositeDisposable);
        });
    }

    private void ThisAddIn_Shutdown(object sender, EventArgs e)
    {
        Globals.ThisAddIn.Application.UnregisterRibbonX(_ribbon, null);

        Configuration.Save();
        InputCache.Save(InputCache);

        _logger.Info($"Configuration and input cache saved on shut down.");

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
            Directory.CreateDirectory(LibraryFolder);
            Directory.CreateDirectory(TmpFolder);

            // try to load nlog config from file, copy from resource if not exist
            NLogConfiguration.CreateIfNotExist();
            NLogConfiguration.Load();

            // try load configuration
            Configuration = Configuration.Load();

            // initialize logger
            _logger = LogManager.GetCurrentClassLogger();

            // load the input cache
            InputCache = InputCache.Load();
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