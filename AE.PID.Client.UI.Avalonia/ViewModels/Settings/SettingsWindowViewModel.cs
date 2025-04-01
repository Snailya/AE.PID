using AE.PID.Client.Core;
using AE.PID.Client.Infrastructure;
using AE.PID.Client.UI.Avalonia.Shared;

namespace AE.PID.Client.UI.Avalonia;

public class SettingsWindowViewModel : ViewModelBase
{
    public AboutViewModel About { get; }
    public AccountSettingViewModel Account { get; }

    #region -- Constructors --

    public SettingsWindowViewModel(NotificationHelper notificationHelper, IConfigurationService configurationService,
        UpdateChecker checker)
    {
        About = new AboutViewModel(notificationHelper, configurationService, checker);
        Account = new AccountSettingViewModel(configurationService);
    }

    #endregion
}