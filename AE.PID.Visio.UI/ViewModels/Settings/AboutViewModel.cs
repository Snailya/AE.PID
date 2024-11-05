using System;
using System.Reactive;
using System.Reactive.Linq;
using AE.PID.Visio.Core.Exceptions;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.UI.Avalonia.Services;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class AboutViewModel : ViewModelBase
{
    #region -- Interactions --

    public Interaction<NewVersionViewModel, bool> ShowNewVersionView { get; } = new();

    #endregion

    public string Version { get; set; }
    public string ProductName { get; set; }

    public ReactiveCommand<Unit, Unit> CheckUpdate { get; }

    #region -- Constructors --

    public AboutViewModel(NotificationHelper notificationHelper, IConfigurationService configurationService,
        IAppUpdateService appUpdateService)
    {
        #region Commands

        CheckUpdate = ReactiveCommand.CreateFromTask(async () =>
        {
            var hasUpdate = await appUpdateService.CheckUpdateAsync(configurationService.RuntimeConfiguration.Version);
            if (hasUpdate != null)
            {
                var viewModel = new NewVersionViewModel
                {
                    Version = hasUpdate.Version,
                    ReleaseNotes = hasUpdate.ReleaseNotes
                };
                if (await ShowNewVersionView.Handle(viewModel))
                {
                    var installerPath = await appUpdateService.DownloadAsync(hasUpdate.DownloadUrl);
                    await appUpdateService.InstallAsync(installerPath);
                }
            }
        });

        CheckUpdate.ThrownExceptions.Subscribe(exception =>
        {
            if (exception is NetworkNotValidException) notificationHelper.Error("检查更新失败", exception.Message);
        });

        #endregion

        #region Start

        ProductName = configurationService.RuntimeConfiguration.ProductName;
        Version = configurationService.RuntimeConfiguration.Version;

        #endregion
    }

    internal AboutViewModel()
    {
        // Design
    }

    #endregion
}