using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using ReactiveMarbles.ObservableEvents;
using System.Reactive.Linq;

namespace AE.PID.Client.UI.Avalonia.Shared;

/// <summary>
///     This class attaches a notification manager control at the top level.
/// </summary>
/// <typeparam name="T"></typeparam>
public class WindowBase<T> : ReactiveWindow<T> where T : WindowViewModelBase
{
    protected WindowBase()
    {
#if DEBUG
        this.AttachDevTools();
#endif

        this.Events().KeyUp.Where(x => x.Key == Key.Enter).Subscribe(_ =>
        {
            if (FocusManager?.GetFocusedElement() is TextBox) FocusManager.ClearFocus();
        });
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        ViewModel!.NotificationManager =
            new WindowNotificationManager(GetTopLevel(this)!);
    }
}