using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;
using AE.PID.Controllers;
using AE.PID.Controllers.Services;
using AE.PID.Models;
using AE.PID.Views;
using Microsoft.Office.Interop.Visio;
using NLog;
using PID.VisioAddIn.Properties;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using Window = System.Windows.Window;

namespace AE.PID;

public partial class ThisAddIn
{
    private readonly object _configurationLock = new();

    public readonly string DataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AE\\PID");

    private Logger _logger;

    private SynchronizationContext _mainContext;

    public Configuration Configuration { get; private set; }

    /// <summary>
    ///     HttpClient should generally be used as a singleton within an application, especially in scenarios where you are
    ///     making multiple HTTP requests. Creating and disposing of multiple instances of HttpClient for each request is not
    ///     recommended, as it can lead to problems such as socket exhaustion and DNS resolution issues.
    /// </summary>
    public HttpClient HttpClient { get; } = new();
    
    /// <summary>
    ///     Initialize the configuration and environment setup.
    /// </summary>
    private void Setup()
    {
        try
        {
            // initialize the data folder
            Directory.CreateDirectory(Path.Combine(Globals.ThisAddIn.DataFolder, "Libraries"));

            // try to load nlog config from file, copy from resource if not exist
            NLogConfiguration.CreateIfNotExist();
            NLogConfiguration.Load();

            // try load configuration
            Configuration = Configuration.Load();

            _logger = LogManager.GetCurrentClassLogger();
        }
        catch (UnauthorizedAccessException)
        {
            _logger.Error("没有权限");
        }
        catch (PathTooLongException)
        {
            _logger.Error("路径名过长，请检查用户自定义安装路径层级结构是否过于复杂。");
        }
        catch (Exception exception)
        {
            _logger.LogUsefulException(exception);
        }
    }

    private void ThisAddIn_Startup(object sender, EventArgs e)
    {
        // associate the main tread with a synchronization context so that the main thread would be achieved using SynchronizationContext.Current.
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
        _mainContext = SynchronizationContext.Current;

        // invoke the setup process immediately
        var setupObservable = Observable.Start(Setup);

        // When setup finished, that means configuration is prepared, request the server to check if there is a valid update.
        // If so, prompt a MessageBox to let user decide whether to update right now.
        // As automatic update not implemented, only open the explorer window to let user know there's a update installer.
        setupObservable
            .Subscribe(_ =>
            {
                Observable
                    .Interval(TimeSpan.FromDays(1))
                    .Merge(Observable.Return<long>(-1))
                    .Select(_ => DateTime.Now)
                    .Where(now => Configuration.NextCheck == null || now > Configuration.NextCheck)
                    .Select(
                        _ => Observable
                            .FromAsync(AppUpdater.GetUpdateAsync)
                    )
                    .Concat() // concat to prevent emitting when previous is not handled by user
                    .Where(result => result.IsUpdateAvailable)
                    .ObserveOn(Scheduler.CurrentThread)
                    .Select(result => new
                        { Info = result, DialogResult = AskForUpdate(result.ReleaseNotes) })
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Do(_ => UpdateAppNextCheckTime())
                    .Where(x => x.DialogResult == DialogResult.Yes)
                    .Select(x => x.Info)
                    .SelectMany(result => AppUpdater.CacheAsync(result.DownloadUrl))
                    .Do(AppUpdater.DoUpdate)
                    .Subscribe(
                        _ => { },
                        ex =>
                        {
                            MessageBox.Show(ex.Message);
                            _logger.Error($"App update listener ternimated accidently. [ERROR MESSAGE] {ex.Message} ");
                        },
                        () => { _logger.Error("App update update listener should never complete."); });
            });

        // Also, check for libraries in another background thread.
        // Library updates are in silent as if the update fails, it may update in next round.
        setupObservable
            .Subscribe(_ =>
            {
                Observable
                    .Interval(Configuration.LibraryConfiguration.CheckInterval)
                    .Merge(Observable.Return<long>(-1))
                    .Select(_ => DateTime.Now)
                    .Where(now =>
                        Configuration.LibraryConfiguration.NextTime == null ||
                        now > Configuration.LibraryConfiguration.NextTime)
                    .SelectMany(
                        _ => Observable
                            .FromAsync(LibraryUpdater.UpdateLibrariesAsync)
                    )
                    .Do(UpdateLibrariesConfiguration)
                    .Subscribe(
                        _ => { },
                        ex =>
                        {
                            MessageBox.Show(ex.Message);
                            _logger.Error(
                                $"Library update listener ternimated accidently. [ERROR MESSAGE] {ex.Message} ");
                        },
                        () => { _logger.Error("Library update listener should never complete."); });
            });

        // initialize the document masters update observable after configuration loaded
        setupObservable
            .Subscribe(_ =>
            {
                Observable
                    .FromEvent<EApplication_DocumentOpenedEventHandler, Document>(
                        handler => Globals.ThisAddIn.Application.DocumentOpened += handler,
                        handler => Globals.ThisAddIn.Application.DocumentOpened -= handler)
                    .Where(document => document.Type == VisDocumentTypes.visTypeDrawing)
                    .Merge(DocumentUpdater.ManuallyInvokeTrigger.Throttle(TimeSpan.FromMilliseconds(300)))
                    .ObserveOn(TaskPoolScheduler.Default)
                    .SelectMany(document => Task.Run(() => DocumentUpdater.GetUpdatesAsync(document)),
                        (document, mappings) => new { Document = document, Mappings = mappings })
                    .Where(data => data.Mappings is not null && data.Mappings.Any())
                    .Select(result => new
                        { Info = result, DialogResult = AskForUpdate("检测到文档模具与库中模具不一致，是否立即更新文档模具？") })
                    .Where(x => x.DialogResult == DialogResult.Yes)
                    .Select(x => x.Info)
                    .ObserveOn(_mainContext)
                    .Subscribe(
                        next =>
                        {
                            // initialize a cts as it's a long time consumed task that user might not want to wait until it finished.
                            var cts = new CancellationTokenSource();

                            // create a window center Visio App
                            var taskProgressWindow = new Window
                            {
                                Width = 300,
                                Height = 150,
                                Content = new TaskProgressView(cts),
                                Title = "更新文档模具"
                            };

                            var unused = new WindowInteropHelper(taskProgressWindow)
                            {
                                Owner = new IntPtr(Globals.ThisAddIn.Application.WindowHandle32)
                            };
                            taskProgressWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                            // do updates in a background thread thought VISIO is STA, but still could do in a single thread without concurrent
                            var updateObservable = Observable.Create<int>(async observer =>
                            {
                                try
                                {
                                    // exceptions occured in other thread could not be caught unless using await
                                    await Task.Run(
                                        () => DocumentUpdater.DoUpdates(next.Document, next.Mappings, cts.Token),
                                        cts.Token);
                                    observer.OnCompleted();
                                }
                                catch (Exception exception)
                                {
                                    observer.OnError(exception);
                                }

                                // if the subscription to this observable is disposed, the task should also be canceled as it is not monitor by anyone
                                return () => cts.Cancel();
                            });

                            var subscription = updateObservable.Subscribe(
                                _ =>
                                {
                                    // ignored
                                },
                                ex =>
                                {
                                    taskProgressWindow.Close();

                                    // if it is a OperationCanceledException it should be treat as normal
                                    if (ex is not OperationCanceledException)
                                        MessageBox.Show(ex.Message);
                                },
                                () =>
                                {
                                    taskProgressWindow.Close();
                                    MessageBox.Show("更新成功");
                                });

                            // Optionally handle subscription disposal, e.g., when the window is closed
                            taskProgressWindow.Closed += (_, _) => subscription.Dispose();

                            // show a progress bar while executing this long running task
                            taskProgressWindow.Show();
                        },
                        ex =>
                        {
                            MessageBox.Show(ex.Message);
                            _logger.Error(
                                $"Document master update listener ternimated accidently. [ERROR MESSAGE] {ex.Message} ");
                        },
                        () => { _logger.Error("Document master update listener should never complete."); });
            });

        setupObservable
            .Subscribe(
                _ =>
                {
                    Exporter.ManuallyInvokeTrigger.Throttle(TimeSpan.FromMilliseconds(300))
                        .Subscribe(
                            _ => { Exporter.SaveAsBom(Globals.ThisAddIn.Application.ActivePage); },
                            ex =>
                            {
                                MessageBox.Show(ex.Message);
                                _logger.Error(
                                    $"Export listener ternimated accidently. [ERROR MESSAGE] {ex.Message} ");
                            },
                            () => { _logger.Error("Export listener should never complete."); }
                        );
                }
            );

        // todo: do not initilize selector in ui thread, as it will block thread, consider to remvoe the window in it outside, thus need to know how to control window close from viemwodel
        setupObservable
                                            .ObserveOn(_mainContext)

            .Subscribe(
                _ =>
                {
                    Selector.ManuallyInvokeTrigger.Throttle(TimeSpan.FromMilliseconds(300))
                                .ObserveOn(_mainContext)
                        .Subscribe(
                            next =>
                            {
                                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
                                Selector.Display();
                            },
                            ex =>
                            {
                                MessageBox.Show(ex.Message);
                                _logger.Error(
                                    $"Select listener ternimated accidently. [ERROR MESSAGE] {ex.Message} ");
                            },
                            () => { _logger.Error("Select listener should never complete."); }
                        );
                }
            );
    }

    private static DialogResult AskForUpdate(string description, string caption = "")
    {
        if (string.IsNullOrEmpty(caption))
            caption = Resources.Product_name;

        return System.Windows.Forms.MessageBox.Show(description, caption, MessageBoxButtons.YesNo);
    }

    private void UpdateLibrariesConfiguration(IEnumerable<Library> libraries)
    {
        lock (_configurationLock)
        {
# if DEBUG
            Configuration.LibraryConfiguration.NextTime = DateTime.Now + TimeSpan.FromMinutes(1);
#else
            Configuration.LibraryConfiguration.NextTime =
 DateTime.Now + Configuration.LibraryConfiguration.CheckInterval;
#endif
            Configuration.LibraryConfiguration.Libraries = new ConcurrentBag<Library>(libraries);
            Configuration.Save(Configuration);
        }
    }

    private void UpdateAppNextCheckTime()
    {
        lock (_configurationLock)
        {
# if DEBUG
            Configuration.NextCheck = DateTime.Now + TimeSpan.FromMinutes(1);
#else
            Configuration.NextCheck = DateTime.Now + TimeSpan.FromDays(1);
#endif
            Configuration.Save(Configuration);
        }
    }

    private void ThisAddIn_Shutdown(object sender, EventArgs e)
    {
        Configuration.Save(Configuration);
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