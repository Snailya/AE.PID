using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    private readonly object _configurationLock = new();
    private Logger _logger;
    private SynchronizationContext _mainContext;

    /// <summary>
    ///     The data folder path in Application Data
    /// </summary>
    public readonly string DataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AE\\PID");

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

    /// <summary>
    ///     Initialize the configuration and environment setup.
    /// </summary>
    private void Setup()
    {
        try
        {
            // initialize the data folder
            Directory.CreateDirectory(Path.Combine(Globals.ThisAddIn.DataFolder, "Libraries"));
            Directory.CreateDirectory(Path.Combine(Globals.ThisAddIn.DataFolder, "Tmp"));

            // try to load nlog config from file, copy from resource if not exist
            NLogConfiguration.CreateIfNotExist();
            NLogConfiguration.Load();

            // try load configuration
            Configuration = Configuration.Load();

            // initialize logger
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
                _logger.Info($"App update listener is running.");

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
                        value => { _logger.Info($"New version of app cached at {value}"); },
                        ex =>
                        {
                            Alert(ex.Message);
                            _logger.Error($"App update listener ternimated accidently. [ERROR MESSAGE] {ex.Message} ");
                        },
                        () => { _logger.Error("App update listener should never complete."); });
            });

        // Also, check for libraries in another background thread.
        // Library updates are in silent as if the update fails, it may update in next round.
        setupObservable
            .Subscribe(_ =>
            {
                _logger.Info($"Library update listener is running.");

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
                            Alert(ex.Message);
                            _logger.Error(
                                $"Library update listener ternimated accidently. [ERROR MESSAGE] {ex.Message} ");
                        },
                        () => { _logger.Error("Library update listener should never complete."); });
            });

        // initialize the document masters update observable after configuration loaded
        setupObservable
            .Subscribe(_ =>
            {
                _logger.Info($"Document master update listener is running.");

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
                    .ObserveOn(_mainContext)
                    .Select(x =>
                    {
                        // close all document 
                        foreach (var document in 
                        Globals.ThisAddIn.Application.Documents.OfType<IVDocument>().Where(x=>x.Type == VisDocumentTypes.visTypeStencil).ToList())
                            document.Close();
   
                        return x.Info;
                    })
                    .Subscribe(
                        next =>
                        {
                            _logger.Info(
                                $"Try updating {next.Document.FullName}, {next.Mappings.Count()} masters need updates.");

                            // initialize a cts as it's a long time consumed task that user might not want to wait until it finished.
                            var cts = new CancellationTokenSource();
                            var vm = new TaskProgressViewModel(cts);

                            // create a window center Visio App
                            var taskProgressWindow = new Window
                            {
                                Width = 300,
                                Height = 150,
                                Content = new TaskProgressView(vm),
                                Title = "更新文档模具"
                            };

                            var unused = new WindowInteropHelper(taskProgressWindow)
                            {
                                Owner = new IntPtr(Globals.ThisAddIn.Application.WindowHandle32)
                            };
                            taskProgressWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                            // create a copy of source file
                            var copied = Path.Combine(Globals.ThisAddIn.DataFolder, "Tmp",
                                Path.ChangeExtension(Path.GetRandomFileName(), "vsdx"));
                            File.Copy(next.Document.FullName, copied);

                            // do updates in a background thread thought VISIO is STA, but still could do in a single thread without concurrent
                            var updateObservable = Observable.Create<int>(async observer =>
                            {
                                try
                                {
                                    var progress = new Progress<int>(observer.OnNext);

                                    await Task.Run(
                                        () => DocumentUpdater.DoUpdatesByOpenXml(copied, next.Mappings, progress,
                                            cts.Token),
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

                            var subscription = updateObservable.ObserveOn(_mainContext).Subscribe(
                                value => { vm.Current = value; },
                                ex =>
                                {
                                    taskProgressWindow.Close();

                                    // if it is a OperationCanceledException it should be treat as normal
                                    if (ex is OperationCanceledException) return;

                                    Alert(ex.Message);
                                    _logger.Error(
                                        $"Update document masters failed. [ERROR MESSAGE] {ex.Message} ");
                                },
                                () =>
                                {
                                    taskProgressWindow.Close();

                                    // open all stencils 
                                    foreach (var path in Globals.ThisAddIn.Configuration.LibraryConfiguration.Libraries.Select(x => x.Path))
                                        Globals.ThisAddIn.Application.Documents.OpenEx(path,
                                            (short)VisOpenSaveArgs.visOpenDocked);

                                    _logger.Info(
                                        $"Document masters updated successfully.");
                                    
                                    Alert("更新成功，请在新文件打开后手动另存。");
                                    Globals.ThisAddIn.Application.Documents.OpenEx(copied,
                                        (short)VisOpenSaveArgs.visOpenRW);
                                });

                            // Optionally handle subscription disposal, e.g., when the window is closed
                            taskProgressWindow.Closed += (_, _) => subscription.Dispose();

                            // show a progress bar while executing this long running task
                            taskProgressWindow.Show();
                        },
                        ex =>
                        {
                            Alert(ex.Message);
                            _logger.Error(
                                $"Document master update listener ternimated accidently. [ERROR MESSAGE] {ex.Message} ");
                        },
                        () => { _logger.Error("Document master update listener should never complete."); });
            });

        setupObservable
            .Subscribe(
                _ =>
                {
                    _logger.Info($"Export listener is running.");

                    Exporter.ManuallyInvokeTrigger.Throttle(TimeSpan.FromMilliseconds(300))
                        .Subscribe(
                            _ => { Exporter.SaveAsBom(Globals.ThisAddIn.Application.ActivePage); },
                            ex =>
                            {
                                Alert(ex.Message);
                                _logger.Error(
                                    $"Export listener ternimated accidently. [ERROR MESSAGE] {ex.Message} ");
                            },
                            () => { _logger.Error("Export listener should never complete."); }
                        );
                }
            );

        // todo: do not initialize selector in ui thread, as it will block thread, consider to remove the window in it outside, thus need to know how to control window close from viewmodel
        setupObservable
            .ObserveOn(_mainContext)
            .Subscribe(
                _ =>
                {
                    _logger.Info($"Select listener is running.");

                    Selector.ManuallyInvokeTrigger.Throttle(TimeSpan.FromMilliseconds(300))
                        .ObserveOn(_mainContext)
                        .Subscribe(
                            _ => { Selector.Display(); },
                            ex =>
                            {
                                Alert(ex.Message);
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
    
    private static DialogResult Alert(string description, string caption = "")
    {
        if (string.IsNullOrEmpty(caption))
            caption = Resources.Product_name;

        return System.Windows.Forms.MessageBox.Show(description, caption);
    }

    private void UpdateLibrariesConfiguration(IEnumerable<Library> libraries)
    {
        lock (_configurationLock)
        {
            TimeSpan checkInterval;
#if DEBUG
            checkInterval = TimeSpan.FromMinutes(1);
#else
            checkInterval = Configuration.LibraryConfiguration.CheckInterval;
#endif
            Configuration.LibraryConfiguration.NextTime = DateTime.Now + checkInterval;
            Configuration.LibraryConfiguration.Libraries = new ConcurrentBag<Library>(libraries);
            Configuration.Save(Configuration);

            _logger.Info($"Configuration updated");
        }
    }

    private void UpdateAppNextCheckTime()
    {
        lock (_configurationLock)
        {
            TimeSpan checkInterval;
#if DEBUG
            checkInterval = TimeSpan.FromMinutes(1);
#else
            checkInterval = TimeSpan.FromDays(1);
#endif
            Configuration.NextCheck = DateTime.Now + checkInterval;
            Configuration.Save(Configuration);

            _logger.Info($"Configuration updated");
        }
    }

    private void ThisAddIn_Shutdown(object sender, EventArgs e)
    {
        Configuration.Save(Configuration);

        _logger.Info($"Configuration saved on close");
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