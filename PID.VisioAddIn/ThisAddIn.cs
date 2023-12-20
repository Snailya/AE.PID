using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Shapes;
using System.Windows.Threading;
using AE.PID.Controllers;
using AE.PID.Controllers.Services;
using AE.PID.Models;
using AE.PID.ViewModels;
using AE.PID.Views;
using Microsoft.Office.Interop.Visio;
using NLog;
using PID.VisioAddIn.Properties;
using Path = System.IO.Path;
using Window = System.Windows.Window;

namespace AE.PID;

public partial class ThisAddIn
{
    private Logger _logger;
    private SynchronizationContext _mainContext;
    private readonly MainWindow _window = new();

    private const long ManuallyInvokeMagicNumber = -255;

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
    public static readonly string TmpFolder = Path.Combine(AppDataFolder, "Tmp");

    /// <summary>
    ///     Configuration has three part: app configuration, library configuration, export settings.
    /// </summary>
    public Configuration Configuration { get; private set; }

    /// <summary>
    ///     HttpClient should generally be used as a singleton within an application, especially in scenarios where you are
    ///     making multiple HTTP requests. Creating and disposing of multiple instances of HttpClient for each request is not
    ///     recommended, as it can lead to problems such as socket exhaustion and DNS resolution issues.
    /// </summary>
    public HttpClient HttpClient { get; } = new();

    private void ThisAddIn_Startup(object sender, EventArgs e)
    {
        // associate the main tread with a synchronization context so that the main thread would be achieved using SynchronizationContext.Current.
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
        _mainContext = SynchronizationContext.Current;

        // title
        _window.Title = Resources.Product_name;

        // make the window auto size to it's content
        _window.SizeToContent = SizeToContent.WidthAndHeight;

        // initialize a reusable window to hold WPF controls
        _ = new WindowInteropHelper(_window)
        {
            Owner = new IntPtr(Globals.ThisAddIn.Application.WindowHandle32)
        };
        _window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

        // invoke the setup process immediately
        var setupObservable = Observable.Start(Setup);

        ListenToAppUpdate(setupObservable);
        ListenToLibraryUpdate(setupObservable);
        ListenToDocumentMasterUpdate(setupObservable);

        ListenToAdvancedSelection(setupObservable);
        ListenToExport(setupObservable);
        ListenToUserSettings(setupObservable);
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
    }

    /// <summary>
    /// When user click the Settings button on Ribbon, display a UI window for user settings.
    /// This observable only displays the view, not focus on any subsequent task.
    /// </summary>
    /// <param name="setupObservable"></param>
    /// <returns></returns>
    private IDisposable ListenToUserSettings(IObservable<Unit> setupObservable)
    {
        return setupObservable.Subscribe(
            _ =>
            {
                ConfigurationUpdater.ManuallyInvokeTrigger.Throttle(TimeSpan.FromMilliseconds(300))
                    .ObserveOn(_mainContext)
                    .Select(_ =>
                    {
                        _window.Content = new UserSettingsView();
                        _window.Show();

                        return Unit.Default;
                    })
                    .Subscribe(
                        _ => { },
                        ex =>
                        {
                            Alert(ex.Message);
                            _logger.Error(ex,
                                $"Configuration updating listener ternimated accidently.");
                        },
                        () => { _logger.Error("Configuration updating listener should never complete."); });
            }
        );
    }

    /// <summary>
    /// When user click the Selection tool button on Ribbon, display a UI window to give user advanced selection.
    /// This observable only displays the view, not focus on any subsequent task.
    /// </summary>
    /// <param name="setupObservable"></param>
    private void ListenToAdvancedSelection(IObservable<Unit> setupObservable)
    {
        setupObservable
            .ObserveOn(_mainContext)
            .Subscribe(
                _ =>
                {
                    _logger.Info($"Select listener is running.");

                    Selector.ManuallyInvokeTrigger.Throttle(TimeSpan.FromMilliseconds(300))
                        .ObserveOn(_mainContext)
                        .Select(_ =>
                        {
                            _window.Content = new ShapeSelectionView();
                            _window.Show();

                            return Unit.Default;
                        })
                        .Subscribe(
                            _ => { },
                            ex =>
                            {
                                Alert(ex.Message);
                                _logger.Error(ex,
                                    $"Select listener ternimated accidently.");
                            },
                            () => { _logger.Error("Select listener should never complete."); }
                        );
                }
            );
    }

    /// <summary>
    /// When user click on Export button on Ribbon, display a export view to let user supply extra info.
    /// All subsequent procedures are invoked by ViewModel.
    /// </summary>
    /// <param name="setupObservable"></param>
    private IDisposable ListenToExport(IObservable<Unit> setupObservable)
    {
        return setupObservable
            .Subscribe(
                _ =>
                {
                    _logger.Info($"Export listener is running.");

                    Exporter.ManuallyInvokeTrigger.Throttle(TimeSpan.FromMilliseconds(300))
                        .ObserveOn(_mainContext)
                        .Select(_ =>
                        {
                            _window.Content = new ExportView();
                            _window.Show(); // this observable only display the view, not focus on any task

                            return Unit.Default;
                        })
                        .Subscribe(
                            _ => { },
                            ex =>
                            {
                                Alert(ex.Message);
                                _logger.Error(ex,
                                    $"Export listener ternimated accidently.");
                            },
                            () => { _logger.Error("Export listener should never complete."); }
                        );
                }
            );
    }

    /// <summary>
    /// Listen to both document open event and user click event to monitor if a document master update is needed.
    /// The update process is done on a background thread using OpenXML, so it is extremely fast.
    /// However, a progress bar still provided in case a long time run needed in the future.
    /// </summary>
    /// <param name="setupObservable"></param>
    private IDisposable ListenToDocumentMasterUpdate(IObservable<Unit> setupObservable)
    {
        return setupObservable
            .Subscribe(_ =>
            {
                _logger.Info($"Document master update listener is running.");

                // document open event
                Observable
                    .FromEvent<EApplication_DocumentOpenedEventHandler, Document>(
                        handler => Globals.ThisAddIn.Application.DocumentOpened += handler,
                        handler => Globals.ThisAddIn.Application.DocumentOpened -= handler)
                    .Where(document => document.Type == VisDocumentTypes.visTypeDrawing)
                    // manually invoke from ribbon
                    .Merge(DocumentUpdater.ManuallyInvokeTrigger.Throttle(TimeSpan.FromMilliseconds(300)))
                    .ObserveOn(TaskPoolScheduler.Default)
                    // compare with library
                    .SelectMany(document => Task.Run(() => DocumentUpdater.GetUpdatesAsync(document)),
                        (document, mappings) => new { Document = document, Mappings = mappings })
                    .Where(data => data.Mappings is not null && data.Mappings.Any())
                    // prompt user decision
                    .Select(result => new
                        { Info = result, DialogResult = AskForUpdate("检测到文档模具与库中模具不一致，是否立即更新文档模具？") })
                    .Where(x => x.DialogResult == DialogResult.Yes)
                    .ObserveOn(_mainContext)
                    // close all document stencils to avoid occupied
                    .Select(x => new { FilePath = DocumentUpdater.Preprocessing(x.Info.Document), x.Info.Mappings })
                    // display a progress bar to do time-consuming operation
                    .Select(data =>
                    {
                        ShowProgressWhileActing(_window,
                            (progress, token) =>
                            {
                                DocumentUpdater.DoUpdatesByOpenXml(data.FilePath, data.Mappings, progress, token);
                                DocumentUpdater.PostProcess(data.FilePath);
                            });
                        return Unit.Default;
                    })
                    .Subscribe(
                        _ => { },
                        ex =>
                        {
                            Alert(ex.Message);
                            _logger.Error(ex,
                                $"Document master update listener ternimated accidently.");
                        },
                        () => { _logger.Error("Document master update listener should never complete."); });
            });
    }

    /// <summary>
    /// Automatically check the server for library updates and done in slient.
    /// The check interval is control by configuration.
    /// </summary>
    /// <param name="setupObservable"></param>
    /// <returns></returns>
    private IDisposable ListenToLibraryUpdate(IObservable<Unit> setupObservable)
    {
        return setupObservable
            .Subscribe(_ =>
            {
                _logger.Info($"Library update listener is running.");

                // auto check observable
                Configuration.LibraryConfiguration.CheckIntervalSubject
                    .Select(Observable.Interval)
                    .Switch()
                    .Merge(Observable.Return<long>(-1))
                    .Where(_ =>
                        Configuration.LibraryConfiguration.NextTime == null ||
                        DateTime.Now > Configuration.LibraryConfiguration.NextTime)
                    // merge with user manually invoke observable
                    .Merge(
                        LibraryUpdater.ManuallyInvokeTrigger.Throttle(TimeSpan.FromMilliseconds(300))
                            .Select(_ => ManuallyInvokeMagicNumber)
                    )
                    // perform check
                    .SelectMany(
                        _ => Observable
                            .FromAsync(LibraryUpdater.UpdateLibrariesAsync),
                        (value, result) => new { InvokeType = value, Result = result }
                    )
                    // notify user if need
                    .Select(data =>
                    {
                        // prompt an alert to let user know update completed if it's invoked by user.
                        if (data.InvokeType == ManuallyInvokeMagicNumber)
                            Alert("更新完毕");

                        _logger.Info($"Updated {data.Result.Count} libraries.");
                        return data.Result;
                    })
                    // as http request may have error, retry for next emit
                    .Retry(3)
                    // for error handling only
                    .Subscribe(
                        _ => { },
                        ex =>
                        {
                            Alert(ex.Message);
                            _logger.Error(ex,
                                $"Library update listener ternimated accidently.");
                        },
                        () => { _logger.Error("Library update listener should never complete."); });
            });
    }

    /// <summary>
    /// When setup finished, that means configuration is prepared, request the server to check if there is a valid update.
    /// If so, prompt a MessageBox to let user decide whether to update right now.
    /// As automatic update not implemented, only open the explorer window to let user know there's a update installer.
    /// </summary>
    /// <param name="setupObservable"></param>
    private IDisposable ListenToAppUpdate(IObservable<Unit> setupObservable)
    {
        return setupObservable
            .Subscribe(_ =>
            {
                _logger.Info($"App update listener is running.");

                // auto check
                Configuration.CheckIntervalSubject
                    .Select(Observable.Interval)
                    .Switch()
                    .Merge(Observable
                        .Return<
                            long>(-1)) // add a immediately value as the interval method emits only after the interval collapse.
                    .Where(_ =>
                        Configuration.NextTime == null ||
                        DateTime.Now > Configuration.NextTime) // ignore if it not till the next check time
                    // merge with user invoke
                    .Merge(
                        AppUpdater.ManuallyInvokeTrigger.Throttle(TimeSpan.FromMilliseconds(300))
                            .Select(_ => ManuallyInvokeMagicNumber)
                    )
                    // check for update
                    .SelectMany(value => Observable.FromAsync(AppUpdater.GetUpdateAsync),
                        (value, result) => new { InvokeType = value, Result = result })
                    // notify user for no update
                    .ObserveOn(_mainContext)
                    .Select(data =>
                    {
                        // prompt an alert to let user know no update needed if it is manually triggered
                        if (data.InvokeType == ManuallyInvokeMagicNumber && !data.Result.IsUpdateAvailable)
                            Alert("这就是最新版本。");

                        return data.Result;
                    })
                    // notify user to decide when to update
                    .Where(data => data.IsUpdateAvailable) // filter only valid update
                    .Select(data => new
                    {
                        Info = data,
                        DialogResult = AskForUpdate("发现新版本。" +
                                                    Environment.NewLine +
                                                    data.ReleaseNotes +
                                                    Environment.NewLine + Environment.NewLine +
                                                    "请在控制面板中卸载旧程序后重新安装。")
                    })
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Where(x => x.DialogResult == DialogResult.Yes)
                    // perform update
                    .SelectMany(result => AppUpdater.CacheAsync(result.Info.DownloadUrl))
                    .Do(path => _logger.Info($"New version of app cached at {path}"))
                    .Do(AppUpdater.PromptManuallyUpdate)
                    // as http request may have error, retry for next emit
                    .Retry(3)
                    .Subscribe(
                        _ => { },
                        ex =>
                        {
                            Alert(ex.Message);
                            _logger.Error(ex, $"App update listener ternimated accidently.");
                        },
                        () => { _logger.Error("App update listener should never complete."); });
            });
    }

    private static DialogResult AskForUpdate(string description, string caption = "")
    {
        if (string.IsNullOrEmpty(caption))
            caption = Resources.Product_name;

        return System.Windows.Forms.MessageBox.Show(description, caption, MessageBoxButtons.YesNo);
    }

    public static void Alert(string description, string caption = "")
    {
        if (string.IsNullOrEmpty(caption))
            caption = Resources.Product_name;

        System.Windows.Forms.MessageBox.Show(description, caption);
    }

    private void ShowProgressWhileActing(Window window, Action<IProgress<int>, CancellationToken> action)
    {
        // initialize a cts as it's a long time consumed task that user might not want to wait until it finished.
        var cts = new CancellationTokenSource();
        var vm = new TaskProgressViewModel(cts);

        // create a window center Visio App
        window.Content = new TaskProgressView(vm);

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
                    _window.Visibility = Visibility.Collapsed;

                    _logger.Error(ex,
                        $"Process failed.");
                    Alert(ex.Message);
                },
                () => { });

        // show a progress bar while executing this long running task
        window.Show();
    }

    private void ThisAddIn_Shutdown(object sender, EventArgs e)
    {
        Configuration.Save(Configuration);

        _logger.Info($"Configuration saved on shut down.");
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