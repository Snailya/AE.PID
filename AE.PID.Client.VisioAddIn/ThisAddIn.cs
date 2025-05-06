using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using AE.PID.Client.Core;
using AE.PID.Client.Core.VisioExt;
using AE.PID.Client.Infrastructure;
using AE.PID.Client.Infrastructure.VisioExt;
using AE.PID.Client.UI.Avalonia;
using AE.PID.Client.UI.Avalonia.Shared;
using AE.PID.Client.UI.Avalonia.VisioExt;
using AE.PID.Visio.Shared;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Office.Core;
using Splat;
using Splat.NLog;

namespace AE.PID.Client.VisioAddIn;

public partial class ThisAddIn : IEnableLogger
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Dispatcher? _dispatcher;
    private IHost _host;
    private Thread _uiThread;
    private static Subject<Unit> AvaloniaSetupUpDone { get; } = new();
    public static IServiceProvider Services { get; private set; }
    public static ScopeManager ScopeManager { get; private set; }

    /// <summary>
    ///     Get the handle of the visio application
    /// </summary>
    /// <returns></returns>
    public static IntPtr GetApplicationHandle()
    {
        return new IntPtr(Globals.ThisAddIn.Application.WindowHandle32);
    }

    private void ThisAddIn_Startup(object sender, EventArgs e)
    {
        // register logger
        Locator.CurrentMutable.UseNLogWithWrappingFullLogger();

        this.Log().Debug("Initializing synchronizetion scheduler at Visio thread...");

        // initialize a scheduler so that we could schedule visio related work on this thread,
        // because the main thread has no synchronization context, a new synchronization context is created
        var mainContext = SynchronizationContext.Current ?? new SynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(mainContext);
        // SynchronizationContext = mainContext;

        // initialize a custom scheduler with VSTO main context
        var dispatcher = Dispatcher.CurrentDispatcher;

        // initialize scheduler manager
        SchedulerManager.VisioScheduler = new DispatcherScheduler(dispatcher);

        this.Log().Info("Synchronization scheduler initialized.");

        // configure IoC
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) => { ConfigureServices(services); })
            .Build();
        Services = _host.Services;

        // register scope manager
        ScopeManager = new ScopeManager(_host.Services);

        _host.RunAsync(_cancellationTokenSource.Token);

        // declare a new UI thread for WPF.
        // notice the apartment state needs to be STA
        this.Log().Debug("Starting a new thread as UI main thread.");

        _uiThread = new Thread(() =>
            {
                AppBuilder.Configure<AvaloniaApp>()
                    .UsePlatformDetect()
                    .WithInterFont()
                    .UseReactiveUI()
                    .LogToTrace()
                    .SetupWithLifetime(new ClassicDesktopStyleApplicationLifetime());

                AvaloniaSetupUpDone.OnCompleted();

                // start and cache the dispatcher
                _dispatcher = Dispatcher.CurrentDispatcher;
                Dispatcher.Run();
            })
            { Name = "UI Thread" };
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();

        this.Log().Info("UI thread started.");

        AvaloniaSetupUpDone.Subscribe(_ => { }, _ => { }, () =>
        {
            // 如果当前的用户ID是空值，提示用户输入UserId
            var configuration = Services.GetRequiredService<IConfigurationService>();
            var ui = Services.GetRequiredService<IUserInteractionService>();
            var vm = Services.GetRequiredService<SettingsWindowViewModel>();
            if (string.IsNullOrEmpty(configuration.GetCurrentConfiguration().UserId))
                ui.Show(vm, GetApplicationHandle());

            // 检查更新
            var checker = Services.GetRequiredService<UpdateChecker>();
            // 2025.04.11: 增加给更新通道选择
            var currentConfiguration = configuration.GetCurrentConfiguration();
            _ = checker.CheckAsync(
                configuration.RuntimeConfiguration.Version,
                currentConfiguration.Server + $"/api/v3/app?channel={(int)currentConfiguration.Channel}");
        });

        _ = PrepareTasks();
    }

    private static async Task PrepareTasks()
    {
        var tasks = Services.GetServices<IBackgroundTask>();
        var queue = Services.GetRequiredService<BackgroundTaskQueue>();

        // 将任务添加到队列
        foreach (var task in tasks) await queue.QueueBackgroundTaskAsync(task, CancellationToken.None);
    }

    private void ThisAddIn_Shutdown(object sender, EventArgs e)
    {
        // Shut down the dispatcher
        this.Log().Debug("Shutting down the dispatcher...");
        _dispatcher?.BeginInvokeShutdown(DispatcherPriority.Background);

        // join the ui thread
        if (_uiThread.IsAlive)
        {
            this.Log().Debug("Joining UI thread...");
            _uiThread.Join();
        }

        _cancellationTokenSource.Cancel();

        this.Log().Debug("Stopping the service...");

        _host.WaitForShutdown();

        this.Log().Info("Addin shutdown.");
    }

    protected override IRibbonExtensibility CreateRibbonExtensibilityObject()
    {
        return new Ribbon();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        LogHost.Default.Debug("Configuring services...");

        // storage service
        services.AddSingleton<IExportService, ExportService>();

        // register configurations
        var fvi = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

        services.AddSingleton<IConfigurationService, ConfigurationService>(provider =>
            new ConfigurationService(provider.GetRequiredService<IExportService>(), fvi.CompanyName, fvi.ProductName,
                fvi.FileVersion));

        // register apis
        services.AddApi<IAppApi>();
        services.AddApi<IStencilApi>();
        services.AddApi<IDocumentApi>();
        services.AddApi<IProjectApi>();
        services.AddApi<IFunctionApi>();
        services.AddApi<IMaterialApi>();
        services.AddApi<ISelectionApi>();

        // register service for background service
        services.AddSingleton<StencilUpdateService>();
        services.AddSingleton<IDocumentUpdateService, DocumentUpdateService>();
        services.AddSingleton<IRecommendedService, RecommendedService>();

        // register for hosted service
        services.AddSingleton<BackgroundTaskQueue>();
        services.AddHostedService<BackgroundTaskExecutor>();

        services.AddSingleton<IBackgroundTask, StencilUpdateTask>(provider =>
        {
            var configuration = Services.GetRequiredService<IConfigurationService>();
            var stencilUpdateService = provider.GetRequiredService<StencilUpdateService>();

            return new StencilUpdateTask(configuration, stencilUpdateService, Globals.ThisAddIn.Application);
        });

        // register user interaction service
        services.AddSingleton<IUserInteractionService, UserInteractionService>();
        services.AddSingleton<UpdateChecker, UpdateChecker>();

        // register for api services
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<IFunctionService, FunctionService>();
        services.AddSingleton<IMaterialService, MaterialService>();

        // register for visio related
        services.AddScoped<ILocalCacheService, VisioSolutionXmlCacheService>(_ =>
            new VisioSolutionXmlCacheService(Globals.ThisAddIn.Application.ActiveDocument));
        services.AddScoped<IDataProvider, VisioProvider>(_ =>
            new VisioProvider(Globals.ThisAddIn.Application.ActiveDocument, SchedulerManager.VisioScheduler));

        //
        services.AddScoped<IProjectResolver, ProjectResolver>();
        services.AddScoped<IFunctionResolver, FunctionResolver>();
        services.AddScoped<IMaterialResolver, MaterialResolver>();

        // register for project explorer
        services.AddScoped<IProjectLocationStore, ProjectLocationStore>();
        services.AddScoped<IFunctionLocationStore, FunctionLocationStore>();
        services.AddScoped<IMaterialLocationStore, MaterialLocationStore>();

        // register for tools
        services.AddScoped<IToolService, ToolService>();

        // register for ViewModels
        services.AddScoped<NotificationHelper, NotificationHelper>();
        services.AddScoped<ProjectExplorerWindowViewModel, ProjectExplorerWindowViewModel>();
        services.AddScoped<ToolsWindowViewModel, ToolsWindowViewModel>();
        services.AddScoped<SettingsWindowViewModel, SettingsWindowViewModel>();
        services.AddScoped<MaterialPaneViewModel, MaterialPaneViewModel>();

        LogHost.Default.Info("Services configured.");
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