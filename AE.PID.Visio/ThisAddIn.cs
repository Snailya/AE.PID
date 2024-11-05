using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using System.Windows.Threading;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Helpers;
using AE.PID.Visio.Services;
using AE.PID.Visio.Shared;
using AE.PID.Visio.Shared.Extensions;
using AE.PID.Visio.Shared.Services;
using AE.PID.Visio.UI.Avalonia;
using AE.PID.Visio.UI.Avalonia.Services;
using AE.PID.Visio.UI.Avalonia.ViewModels;
using AE.PID.Visio.UI.Avalonia.Views;
using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Office.Core;
using Splat;
using Splat.NLog;

namespace AE.PID.Visio;

public partial class ThisAddIn : IEnableLogger
{
    private IHost _host;
    private Thread _uiThread;
    public static IServiceProvider Services { get; private set; }

    private void ThisAddIn_Startup(object sender, EventArgs e)
    {
#if DEBUG

        DebugExt.Log("This AddIn Startup");

#endif

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

        _host.RunAsync();

        // declare a new UI thread for WPF. Notice the apartment state needs to be STA
        this.Log().Debug("Starting a new thread as UI main thread.");

        var avaloniaSetupUpDone = new Subject<Unit>();
        _uiThread = new Thread(() =>
            {
                AppBuilder.Configure<AvaloniaApp>()
                    .UsePlatformDetect()
                    .WithInterFont()
                    .UseReactiveUI()
                    .LogToTrace()
                    .SetupWithoutStarting();

                avaloniaSetupUpDone.OnCompleted();

#if DEBUG
                DebugExt.Log("UI setup up done.");
#endif

                Dispatcher.Run();
            })
            { Name = "UI Thread" };
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();

        this.Log().Info("UI thread started.");

        avaloniaSetupUpDone.Subscribe(_ => { }, e => { }, () =>
        {
            // 如果当前的用户ID是空值，提示用户输入UserId
            var configuration = Services.GetRequiredService<IConfigurationService>();
            if (string.IsNullOrEmpty(configuration.GetCurrentConfiguration().UserId))
                WindowHelper.Show<SettingsWindow, SettingsWindowViewModel>();
        });
    }

    private void ThisAddIn_Shutdown(object sender, EventArgs e)
    {
        _host.StopAsync();

        this.Log().Debug("Shutting down the dispatcher of the UI thread...");

        Dispatcher.FromThread(_uiThread)?.InvokeShutdown();

        if (!_uiThread.IsAlive) return;

        this.Log().Debug("Joining UI thread...");
        _uiThread.Join();
    }

    protected override IRibbonExtensibility CreateRibbonExtensibilityObject()
    {
        return new Ribbon();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        LogHost.Default.Debug("Configuring services...");

        // storage service
        services.AddSingleton<IStorageService, StorageService>();

        // register configurations
        var fvi = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
        services.AddSingleton<IConfigurationService, ConfigurationService>(provider =>
            new ConfigurationService(provider.GetRequiredService<IStorageService>(), fvi.ProductName, fvi.FileVersion));

        // register apis
        services.AddApi<IAppApi>();
        services.AddApi<IStencilApi>();
        services.AddApi<IDocumentApi>();
        services.AddApi<IProjectApi>();
        services.AddApi<IFunctionApi>();
        services.AddApi<IMaterialApi>();
        services.AddApi<ISelectionApi>();

        // register service for background service
        services.AddSingleton<IAppUpdateService, AppUpdateService>();
        services.AddSingleton<StencilUpdateService>();
        services.AddSingleton<IDocumentUpdateService, DocumentUpdateService>();
        services.AddSingleton<IRecommendedService, RecommendedService>();

        // register for hosted service
        services.AddHostedService<AppUpdateHostedService>();

        services.AddHostedService<StencilUpdateBackgroundService>(provider =>
            new StencilUpdateBackgroundService(provider.GetRequiredService<IConfigurationService>(),
                provider.GetRequiredService<StencilUpdateService>(), Globals.ThisAddIn.Application));

        // services.AddSingleton<BackgroundTaskQueue>(_ => new BackgroundTaskQueue(1));
        // services.AddHostedService<QueuedBackgroundService>();
        // services.AddHostedService<DocumentUpdateHostedService>(provider =>
        //     new DocumentUpdateHostedService(provider.GetRequiredService<IHostApplicationLifetime>(),
        //         provider.GetRequiredService<BackgroundTaskQueue>(),
        //         provider.GetRequiredService<IDocumentUpdateService>(), Globals.ThisAddIn.Application));

        // register for api services
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<IFunctionService, FunctionService>();
        services.AddSingleton<IMaterialService, MaterialService>();

        // register for visio related
        services.AddScoped<ILocalCacheService, LocalCacheService>(_ =>
            new LocalCacheService(Globals.ThisAddIn.Application.ActiveDocument));
        services.AddScoped<IVisioService, VisioService>(_ =>
            new VisioService(Globals.ThisAddIn.Application.ActiveDocument, SchedulerManager.VisioScheduler));

        //
        services.AddScoped<IMaterialResolver, MaterialResolver>();

        // register for project explorer
        services.AddScoped<IProjectStore, ProjectStore>();
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