using System;
using System.Reactive.Linq;
using AE.PID.Visio.UI.Avalonia.Services;
using Avalonia.Controls.Notifications;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public abstract class WindowViewModelBase : ViewModelBase
{
    protected WindowViewModelBase()
    {
    }

    protected WindowViewModelBase(NotifyService? notifyService, string? route = null)
    {
        notifyService?
            .Notifications
            .WhereNotNull()
            .Where(x => x.Route == null || x.Route == route)
            .Subscribe(x =>
                NotificationManager?.Show(x));
    }

    public WindowNotificationManager? NotificationManager { get; set; }
}