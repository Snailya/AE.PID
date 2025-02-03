using System;
using System.Reactive.Concurrency;
using AE.PID.Client.Core;
using AE.PID.Client.Infrastructure;
using AE.PID.Client.Infrastructure.VisioExt;
using AE.PID.Client.UI.Avalonia;
using AE.PID.UI.Avalonia;
using AE.PID.UI.Shared;
using AE.PID.Visio.UI.Design.Services;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using Splat;

namespace AE.PID.Visio.UI.Design;

public sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new ProjectExplorerWindow()
            {
                DataContext = ViewModelLocator.Create<ProjectExplorerWindowViewModel>()
            };
            
            window.Show(desktop.MainWindow);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
    {
        SchedulerManager.VisioScheduler = ThreadPoolScheduler.Instance;

        ConfigureService();

        return AppBuilder.Configure<AvaloniaApp>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
    }


    private static void ConfigureService()
    {
        // storage service
        Locator.CurrentMutable.RegisterConstant<IStorageService>(new MoqStorageService());
        Locator.CurrentMutable.RegisterConstant<ILocalCacheService>(new MoqLocalCacheService());

        // register configurations
        Locator.CurrentMutable.RegisterLazySingleton<IConfigurationService>(() => new MoqConfigurationService());

        // register for api services
        Locator.CurrentMutable.RegisterConstant<IProjectService>(
            new MoqProjectService());
        Locator.CurrentMutable.RegisterConstant<IMaterialService>(
            new MoqMaterialService());

        // register for data provider
        Locator.CurrentMutable.RegisterConstant<IDataProvider>(
            new MoqDataProvider());

        // register for resolver
        Locator.CurrentMutable.Register<IProjectResolver>(() =>
            new ProjectResolver(Locator.Current.GetService<IProjectService>()!,
                Locator.Current.GetService<ILocalCacheService>()!));
        Locator.CurrentMutable.Register<IMaterialResolver>(() =>
            new MaterialResolver(Locator.Current.GetService<IMaterialService>()!,
                Locator.Current.GetService<ILocalCacheService>()!));

        // register for project explorer
        Locator.CurrentMutable.Register<IProjectLocationStore>(() =>
            new ProjectLocationStore(
                Locator.Current.GetService<IDataProvider>()!,
                Locator.Current.GetService<IProjectResolver>()!,
                Locator.Current.GetService<ILocalCacheService>()!));
        Locator.CurrentMutable.Register<IFunctionLocationStore>(() =>
            new FunctionLocationStore(
                Locator.Current.GetService<IDataProvider>()!,
                Locator.Current.GetService<IFunctionResolver>()!,
                Locator.Current.GetService<ILocalCacheService>()!));
        Locator.CurrentMutable.Register<IMaterialLocationStore>(() =>
            new MaterialLocationStore(
                Locator.Current.GetService<IDataProvider>()!,
                Locator.Current.GetService<IFunctionLocationStore>()!,
                Locator.Current.GetService<IMaterialResolver>()!,
                Locator.Current.GetService<ILocalCacheService>()!,
                Locator.Current.GetService<IStorageService>()!));

        // register for tools
        Locator.CurrentMutable.Register(() => new NotificationHelper());

        // register for ViewModels

        Locator.CurrentMutable.Register(() => new ProjectExplorerWindowViewModel(
            Locator.Current.GetService<NotificationHelper>()!,
            Locator.Current.GetService<IProjectService>()!, Locator.Current.GetService<IFunctionService>()!,
            Locator.Current.GetService<IMaterialService>()!,
            Locator.Current.GetService<IProjectLocationStore>()!, Locator.Current.GetService<IFunctionLocationStore>()!,
            Locator.Current.GetService<IMaterialLocationStore>()!));
        Locator.CurrentMutable.Register(() =>
            new SettingsWindowViewModel(Locator.Current.GetService<NotificationHelper>()!,
                Locator.Current.GetService<IConfigurationService>()!,
                Locator.Current.GetService<IAppUpdateService>()!));
    }
}