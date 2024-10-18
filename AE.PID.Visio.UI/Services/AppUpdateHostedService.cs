using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using AE.PID.Visio.UI.Avalonia.ViewModels;
using AE.PID.Visio.UI.Avalonia.Views;
using Microsoft.Extensions.Hosting;
using ReactiveUI;
using Splat;

namespace AE.PID.Visio.UI.Avalonia.Services;

public class AppUpdateHostedService(
    IAppUpdateService appUpdateService,
    IConfigurationService configurationService)
    : IHostedService, IEnableLogger
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.Log().Info(
            $"App update service is starting. The current app version is {configurationService.RuntimeConfiguration.Version}.");

        var configuration = configurationService.GetCurrentConfiguration();

        // 首先检查是否已经有pending update
        if (configuration.PendingAppUpdate != null && new Version(configuration.PendingAppUpdate.Version) >
            new Version(configurationService.RuntimeConfiguration.Version) &&
            !string.IsNullOrEmpty(configuration.PendingAppUpdate.InstallerPath))
        {
            AskForInstallAsync(configuration.PendingAppUpdate);
        }
        // 如果没有，重新检查更新
        else
        {
            var observeUpdate =
                Observable.StartAsync(async () =>
                    {
                        if (string.IsNullOrEmpty(configuration.PendingAppUpdate?.DownloadUrl))
                            return await appUpdateService.CheckUpdateAsync(
                                configurationService.RuntimeConfiguration.Version);
                        return configuration.PendingAppUpdate;
                    })
                    .Where(x => x != null)
                    .Select(x => x!)
                    .Do(x => configurationService.UpdateProperty(i => i.PendingAppUpdate!, x));

            var observeDownload = observeUpdate
                .SelectMany(x => appUpdateService.DownloadAsync(x.DownloadUrl)).Do(x =>
                    configurationService.UpdateProperty(i => i.PendingAppUpdate!.InstallerPath, x));

            var observeUserDecision = observeUpdate
                .SelectMany(async x => await AskForInstallAsync(x));

            observeDownload.Zip(observeUserDecision)
                .Where(x => x.Second && !string.IsNullOrEmpty(x.First))
                .Subscribe(x => { appUpdateService.InstallAsync(x.First); });
        }


        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.Log().Info("Stopping app update hosted service.");
        return Task.CompletedTask;
    }

    private static Task<bool> AskForInstallAsync(PendingAppUpdate pendingAppUpdate)
    {
        var taskCompletionSource = new TaskCompletionSource<bool>();
        var task = taskCompletionSource.Task;
        // prompt a window to ask for updates
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

            // if user choose to update right now, invoke the installation
            viewModel.Confirm.Subscribe(_ => { taskCompletionSource.SetResult(true); });

            // if the user choose to not, save the update to the configuration and pending until next time open the application
            viewModel.Cancel.Subscribe(_ => { taskCompletionSource.SetResult(false); });
        });

        return task;
    }
}