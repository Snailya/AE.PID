using AE.PID.Client.Core;
using AE.PID.UI.Shared;

namespace AE.PID.UI.Avalonia;

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