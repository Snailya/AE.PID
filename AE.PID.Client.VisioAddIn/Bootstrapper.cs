using System;
using System.Diagnostics;
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
using Splat;
using Splat.NLog;

namespace AE.PID.Client.VisioAddIn;

public sealed class Bootstrapper : IServiceBridge, IDisposable, IEnableLogger
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ManualResetEvent _initSignal = new(false);
    private BackgroundTaskQueue? _backgroundTaskQueue;
    private Dispatcher? _dispatcher;
    private bool _disposed;

    private IHost? _host;

    private ScopeManager? _scopeManager;
    private Thread? _uiThread;

    public Bootstrapper()
    {
        // 初始化日志系统的设置 (可以放在构造函数中，因为通常是轻量级的配置)
        Locator.CurrentMutable.UseNLogWithWrappingFullLogger();

        //为 AppDomain 的 UnhandledException 增加处理方法 (尽早注册，放在构造函数中)
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // 启动异步初始化流程
        Task.Run(() => RunAsync(_cts.Token));
    }

    public void Dispose()
    {
        ShutdownAsync().Wait();
    }

    public T GetRequiredService<T>() where T : class
    {
        return _host!.Services.GetRequiredService<T>();
    }

    public IServiceScope CreateScope()
    {
        return _host!.Services.CreateScope();
    }

    public IServiceScope GetScope(object obj)
    {
        return _scopeManager!.GetScope(obj);
    }

    public void ReleaseScope(object obj)
    {
        _scopeManager!.ReleaseScope(obj);
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            // 创建专用 UI 线程
            _uiThread = new Thread(() =>
            {
                // 创建同步上下文
                SynchronizationContext.SetSynchronizationContext(
                    new DispatcherSynchronizationContext());
                _dispatcher = Dispatcher.CurrentDispatcher;

                // 初始化 UI 组件
                AppBuilder.Configure<AvaloniaApp>()
                    .UsePlatformDetect()
                    .WithInterFont()
                    .UseReactiveUI()
                    .LogToTrace()
                    .SetupWithLifetime(new ClassicDesktopStyleApplicationLifetime());

                // start and cache the dispatcher
                Dispatcher.Run();
            })
            {
                Name = "UI Thread",
                IsBackground = true
            };
            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.Start();

            // configure IoC
            _host = await Task.Run(() => Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) => { ConfigureServices(services); })
                .Build(), ct);
            await _host.StartAsync(ct);

            // register scope manager
            _scopeManager = new ScopeManager(_host.Services);

            // 启动任务处理队列
            await StartBackgroundTaskQueueAsync(ct);

            // 初始化完成通知（线程安全）
            _initSignal.Set();
        }
        catch (OperationCanceledException)
        {
            this.Log().Warn("Bootstrapper initialization was canceled");
            CleanupResources();
        }
        catch (Exception e)
        {
            this.Log().Error(e, "Bootstrapper failed");
            CleanupResources();

            throw;
        }
    }

    // 外部等待初始化完成
    public async Task WaitForReadyAsync(TimeSpan timeout)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        var timeoutToken = timeoutCts.Token;

        await Task.Run(() =>
        {
            WaitHandle.WaitAny([_initSignal, timeoutToken.WaitHandle]);
            if (timeoutToken.IsCancellationRequested)
                throw new TimeoutException("Bootstrapper initialization timeout");
        }, timeoutToken);
    }

    // 关闭处理（Office 关闭时调用）
    public async Task ShutdownAsync()
    {
        if (_disposed) return;

        // 触发取消
        _cts.Cancel();

        try
        {
            // 尽管前面已经调用了CancellationTokenSource的取消方法，但是由于不清楚IHost内部的实现方式，所以未必能保证Host被正确停止，因此，又显示调用了StopAsync方法
            // 向StopAsync方法中传入一个超时token，以防止停止过程无限期地挂起。如果超时，则不再尝试gracefully停止服务，而是使用dispose方法强制释放相关资源
            using var shutdownCts = new CancellationTokenSource(5000);
            if (_host != null)
            {
                await _host.StopAsync(shutdownCts.Token);
                _host.Dispose();
            }

            // 关闭 UI 线程
            if (_uiThread is { IsAlive: true })
            {
                _dispatcher!.InvokeShutdown();
                if (!_uiThread.Join(5000)) _uiThread.Interrupt();
            }
        }
        catch (OperationCanceledException)
        {
            this.Log().Warn("Graceful shutdown timeout");
        }
    }

    private void CleanupResources()
    {
        if (_disposed) return;

        _initSignal.Dispose();
        _scopeManager?.Dispose();
        _host?.Dispose();

        _disposed = true;
    }


    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            this.Log().Fatal(ex, "unhandled exception");
        else
            this.Log().Fatal(e.ExceptionObject);
    }

    private async Task StartBackgroundTaskQueueAsync(CancellationToken token)
    {
        var tasks = _host!.Services.GetServices<IBackgroundTask>();
        _backgroundTaskQueue = _host.Services.GetRequiredService<BackgroundTaskQueue>();

        // 将任务添加到队列
        foreach (var task in tasks) await _backgroundTaskQueue.QueueBackgroundTaskAsync(task, token);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        this.Log().Debug("Configuring services...");

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
            var configuration = provider.GetRequiredService<IConfigurationService>();
            var stencilUpdateService = provider.GetRequiredService<StencilUpdateService>();

            return new StencilUpdateTask(configuration, stencilUpdateService, Globals.ThisAddIn.Application);
        });

        // register user interaction service
        services.AddSingleton<IUserInteractionService, UserInteractionService>();
        services.AddSingleton<UpdateChecker, UpdateChecker>();

        // register for ViewModels that is non-relevant to the document.
        services.AddSingleton<SettingsWindowViewModel, SettingsWindowViewModel>();

        // register for api services
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<IFunctionService, FunctionService>();
        services.AddSingleton<IMaterialService, MaterialService>();

        // for any services that extract data from a visio document (using any visio document as parameter input), it's lifetime should strongly related to that input document. 
        // therefore, all the related services should be registered as scoped, so that these services could be get by scope manager and properly disposed after no reference count to it.
        // register for visio related
        services.AddScoped<ILocalCacheService, VisioSolutionXmlCacheService>(_ =>
            new VisioSolutionXmlCacheService(Globals.ThisAddIn.Application.ActiveDocument));
        services.AddScoped<IDataProvider, VisioProvider>(_ =>
            new VisioProvider(Globals.ThisAddIn.Application.ActiveDocument, SchedulerManager.VisioScheduler));

        services.AddScoped<IProjectResolver, ProjectResolver>();
        services.AddScoped<IFunctionResolver, FunctionResolver>();
        services.AddScoped<IMaterialResolver, MaterialResolver>();

        // register for project explorer
        services.AddScoped<IProjectLocationStore, ProjectLocationStore>();
        services.AddScoped<IFunctionLocationStore, FunctionLocationStore>();
        services.AddScoped<IMaterialLocationStore, MaterialLocationStore>();

        // register for tools
        services.AddScoped<IToolService, ToolService>();

        // register for ViewModels that strongly bound to the target document
        services.AddScoped<NotificationHelper, NotificationHelper>();
        services.AddScoped<ProjectExplorerWindowViewModel, ProjectExplorerWindowViewModel>();
        services.AddScoped<ToolsWindowViewModel, ToolsWindowViewModel>();
        services.AddScoped<MaterialPaneViewModel, MaterialPaneViewModel>();

        this.Log().Debug("Services configured.");
    }
}