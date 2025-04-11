using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Linq;
using AE.PID.Client.Core;
using AE.PID.Client.UI.Avalonia.Shared;
using AE.PID.Core;
using DocumentFormat.OpenXml.InkML;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public class GeneralSettingsViewModel : ViewModelBase
{
    private readonly IConfigurationService _configurationService;
    private string _userId = string.Empty;
    private VersionChannel _channel;

    public Array ChannelOptions => Enum.GetValues(typeof(VersionChannel));
    
    [Required]
    public string UserId
    {
        get => _userId;
        set => this.RaiseAndSetIfChanged(ref _userId, value);
    }

    public string DeviceId { get; set; }

    public VersionChannel Channel
    {
        get => _channel;
        set => this.RaiseAndSetIfChanged(ref _channel, value);
    }

    private void DoUpdateUserId(string value)
    {
        _configurationService.UpdateProperty(x => x.UserId, value);
    }

    private void DoUpdateChannel(VersionChannel value)
    {
        _configurationService.UpdateProperty(x => x.Channel, value);
    }

    
    #region -- Constructors --

    public GeneralSettingsViewModel(IConfigurationService configurationService)
    {
        _configurationService = configurationService;

        DeviceId = configurationService.RuntimeConfiguration.UUID;
        UserId = configurationService.GetCurrentConfiguration().UserId;
        Channel = configurationService.GetCurrentConfiguration().Channel;
        
        this.ObservableForProperty(x => x.UserId, false, true)
            .Throttle(TimeSpan.FromMilliseconds(450))
            .Select(x => x.Value)
            .Subscribe(DoUpdateUserId);
        
        this.ObservableForProperty(x => x.Channel, false, true)
            .Throttle(TimeSpan.FromMilliseconds(450))
            .Select(x => x.Value)
            .Subscribe(DoUpdateChannel);
    }

    internal GeneralSettingsViewModel()
    {
        // Design
    }

    #endregion
}