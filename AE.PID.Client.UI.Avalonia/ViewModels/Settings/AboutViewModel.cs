using System;
using System.Reactive;
using AE.PID.Client.Core;
using AE.PID.Client.Infrastructure;
using AE.PID.Client.UI.Avalonia.Shared;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public class AboutViewModel : ViewModelBase
{
    private bool? _hasUpdate;
    public string Version { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public ReactiveCommand<Unit, Unit> CheckUpdate { get; }

    public bool? HasUpdate
    {
        get => _hasUpdate;
        set => this.RaiseAndSetIfChanged(ref _hasUpdate, value);
    }


    #region -- Constructors --

    public AboutViewModel(NotificationHelper notificationHelper, IConfigurationService configuration,
        UpdateChecker checker)
    {
        #region Start

        ProductName = configuration.RuntimeConfiguration.ProductName;
        Version = configuration.RuntimeConfiguration.Version;

        #endregion

        #region Commands

        CheckUpdate = ReactiveCommand.CreateFromTask(async () =>
            {
                var currentConfiguration = configuration.GetCurrentConfiguration();
                if (await checker.CheckAsync(Version,
                        currentConfiguration.Server + $"/api/v3/app?channel{currentConfiguration.Channel}"))
                HasUpdate = false;
            }
        );

        CheckUpdate.ThrownExceptions.Subscribe(exception =>
        {
            if (exception is NetworkNotValidException) notificationHelper.Error("检查更新失败", exception.Message);
        });

        #endregion
    }

    internal AboutViewModel()
    {
        // Design
    }

    #endregion
}