using System;
using System.Reactive.Linq;
using AE.PID.Visio.UI.Avalonia.ViewModels;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.ReactiveUI;
using ReactiveMarbles.ObservableEvents;
using Key = Avalonia.Input.Key;

namespace AE.PID.Visio.UI.Avalonia.Views;

/// <summary>
///     This class attach a notification manager control at the top level.
/// </summary>
/// <typeparam name="T"></typeparam>
public class WindowBase<T> : ReactiveWindow<T> where T : WindowViewModelBase
{
    protected WindowBase()
    {
        Topmost = true;
        
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