using System.Reactive.Linq;
using Avalonia.Controls.Notifications;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia.Shared;

public abstract class WindowViewModelBase : ViewModelBase
{
    protected WindowViewModelBase()
    {
    }

    protected WindowViewModelBase(NotificationHelper? notificationHelper, string? route = null)
    {
        notificationHelper?
            .Notifications
            .WhereNotNull()
            .Where(x => x.Route == null || x.Route == route)
            .Subscribe(x =>
                NotificationManager?.Show(x));
    }

    public WindowNotificationManager? NotificationManager { get; set; }
}