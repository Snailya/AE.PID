using System;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Linq;
using AE.PID.Visio.Core.Interfaces;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class AccountSettingViewModel : ViewModelBase
{
    private readonly IConfigurationService _configurationService;
    private string _userId;

    [Required]
    public string UserId
    {
        get => _userId;
        set => this.RaiseAndSetIfChanged(ref _userId, value);
    }

    public string DeviceId { get; set; }

    private void DoUpdateUserId(string value)
    {
        _configurationService.UpdateProperty(x => x.UserId, value);
    }

    #region -- Constructors --

    public AccountSettingViewModel(IConfigurationService configurationService)
    {
        _configurationService = configurationService;

        DeviceId = configurationService.RuntimeConfiguration.UUID;

        configurationService.Configuration.Subscribe(v => { UserId = v.UserId; });

        this.ObservableForProperty(x => x.UserId, skipInitial: true)
            .Throttle(TimeSpan.FromMilliseconds(450))
            .Select(x => x.Value)
            .Subscribe(DoUpdateUserId);
    }

    internal AccountSettingViewModel()
    {
        // Design
    }

    #endregion
}