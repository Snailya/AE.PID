using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using AE.PID.Client.UI.Avalonia;
using ReactiveUI;
using Splat;

namespace AE.PID.Client.VisioAddIn;

public class AppUpdateTask(
    IAppUpdateService appUpdateService,
    IConfigurationService configurationService)
    : BackgroundTaskBase, IEnableLogger, IDisposable
{
    private readonly CompositeDisposable _cleanUp = new();

    public override string TaskName { get; } = "App Update Task";

    public void Dispose()
    {
        _cleanUp.Dispose();
    }

    public override async Task ExecuteAsync(CancellationToken cts)
    {
        await base.ExecuteAsync(cts);

        this.Log().Info(
            $"App update service is starting. The current app version is {configurationService.RuntimeConfiguration.Version}.");

        var configuration = configurationService.GetCurrentConfiguration();

        // 每一次都重新检查是否有更新，因为如果用户拒绝了某次更新，然后又很久才打开这个app，会导致已缓存的app安装包失去意义。
        // 极端的情况是撤包了，还会提示用户安装这个已经撤回的包。
        var updateInfo = await appUpdateService.CheckUpdateAsync(
            configurationService.RuntimeConfiguration.Version);
        if (updateInfo == null) return;

        // 如果存在更新，则首先检查本地安装包是否和这个更新指向同一个版本，如果指向同一个版本，则无需下载，只需要直接安装。
        if (!string.IsNullOrEmpty(configuration.PendingAppUpdate?.InstallerPath) &&
            updateInfo.Version == configuration.PendingAppUpdate?.Version)
        {
            _ = AskForInstallAsync(configuration.PendingAppUpdate);
            return;
        }

        // 如果本地已经缓存的安装包已经过时，或者没有安装包，则需要重新下载
        var observeDownload = Observable.StartAsync(() => appUpdateService.DownloadAsync(updateInfo.DownloadUrl))
            .Do(x => configurationService.UpdateProperty(i => i.PendingAppUpdate!.InstallerPath, x));

        // 在重新下载的同时询问用户是否需要更新。
        var observeUserDecision = Observable.StartAsync(() => AskForInstallAsync(updateInfo));

        observeDownload.Zip(observeUserDecision)
            .Where(x => x.Second && !string.IsNullOrEmpty(x.First))
            .Subscribe(x =>
            {
                this.Log().Info("The installation process is going to start.");
                appUpdateService.InstallAsync(x.First);
            }, e =>
            {
                if (e is UrlNotValidException)
                    configurationService.UpdateProperty(i => i.PendingAppUpdate!.InstallerPath, string.Empty);
            })
            .DisposeWith(_cleanUp);
    }


    private static Task<bool> AskForInstallAsync(PendingAppUpdate pendingAppUpdate)
    {
        var taskCompletionSource = new TaskCompletionSource<bool>();
        var task = taskCompletionSource.Task;
        // prompt a window to ask for updates

        ThisAddIn.AvaloniaSetupUpDone.Subscribe(_ => { }, e => { }, () =>
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                var viewModel = new NewVersionViewModel
                {
                    Version = pendingAppUpdate.Version,
                    ReleaseNotes = pendingAppUpdate.ReleaseNotes
                };
                var window = new NewVersionWindow
                {
                    DataContext = viewModel
                };
                window.Show();

                // if a user chooses to update right now, invoke the installation
                viewModel.Confirm.Subscribe(_ => { taskCompletionSource.SetResult(true); });

                // if the user choose to not, save the update to the configuration and pending until next time open the application
                viewModel.Cancel.Subscribe(_ => { taskCompletionSource.SetResult(false); });
            }));

        return task;
    }
}