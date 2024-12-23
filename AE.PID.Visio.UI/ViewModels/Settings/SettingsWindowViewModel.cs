﻿using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.UI.Avalonia.Services;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class SettingsWindowViewModel : ViewModelBase
{
    public AboutViewModel About { get; }
    public AccountSettingViewModel Account { get; }

    #region -- Constructors --

    public SettingsWindowViewModel(NotificationHelper notificationHelper, IConfigurationService configurationService,
        IAppUpdateService appUpdateService)
    {
        About = new AboutViewModel(notificationHelper, configurationService, appUpdateService);
        Account = new AccountSettingViewModel(configurationService);
    }

    internal SettingsWindowViewModel()
    {
        // Design
    }

    #endregion
}