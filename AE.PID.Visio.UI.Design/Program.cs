using System;
using System.Reactive.Concurrency;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Shared;
using AE.PID.Visio.Shared.Services;
using AE.PID.Visio.UI.Avalonia;
using AE.PID.Visio.UI.Avalonia.Services;
using AE.PID.Visio.UI.Avalonia.ViewModels;
using AE.PID.Visio.UI.Design.Services;
using Avalonia;
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
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
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

        // register configurations
        Locator.CurrentMutable.RegisterConstant<IConfigurationService>(
            new ConfigurationService(Locator.Current.GetService<IStorageService>()!, "", ""));

        // register for api services
        Locator.CurrentMutable.RegisterConstant<IProjectService>(
            new ProjectService(new ApiFactory<IProjectApi>(Locator.Current.GetService<IConfigurationService>()!)));
        Locator.CurrentMutable.RegisterConstant<IFunctionService>(
            new FunctionService(new ApiFactory<IFunctionApi>(Locator.Current.GetService<IConfigurationService>()!)));
        Locator.CurrentMutable.RegisterConstant<IMaterialService>(
            new MaterialService(new ApiFactory<IMaterialApi>(Locator.Current.GetService<IConfigurationService>()!)));


        // register for visio related
        Locator.CurrentMutable.Register<IVisioService>(() =>
            new MoqVisioService());

        // register for project explorer
        Locator.CurrentMutable.Register<IProjectStore>(() =>
            new ProjectStore(Locator.Current.GetService<IProjectService>()!,
                Locator.Current.GetService<IVisioService>()!, Locator.Current.GetService<ILocalCacheService>()!));
        Locator.CurrentMutable.Register<IFunctionLocationStore>(() =>
            new FunctionLocationStore(Locator.Current.GetService<IFunctionService>()!,
                Locator.Current.GetService<IVisioService>()!));
        Locator.CurrentMutable.Register<IMaterialLocationStore>(() =>
            new MaterialLocationStore(Locator.Current.GetService<IVisioService>()!,
                Locator.Current.GetService<IFunctionLocationStore>()!,
                Locator.Current.GetService<IMaterialResolver>()!,
                Locator.Current.GetService<ILocalCacheService>()!,
                Locator.Current.GetService<IStorageService>()!));

        // register for tools
        Locator.CurrentMutable.Register<IToolService>(() => new MoqToolService());

        Locator.CurrentMutable.Register(() => new NotifyService());

        // register for ViewModels

        Locator.CurrentMutable.Register(() => new ProjectExplorerWindowViewModel(
            Locator.Current.GetService<NotifyService>()!, Locator.Current.GetService<IProjectService>()!,
            Locator.Current.GetService<IFunctionService>()!, Locator.Current.GetService<IMaterialService>()!,
            Locator.Current.GetService<IProjectStore>()!, Locator.Current.GetService<IFunctionLocationStore>()!,
            Locator.Current.GetService<IMaterialLocationStore>()!));
        Locator.CurrentMutable.Register(() => new ToolsWindowViewModel(Locator.Current.GetService<IToolService>()!));
        Locator.CurrentMutable.Register(() =>
            new SettingsWindowViewModel(Locator.Current.GetService<NotifyService>()!,
                Locator.Current.GetService<IConfigurationService>()!,
                Locator.Current.GetService<IAppUpdateService>()!));
    }
}