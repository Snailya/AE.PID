using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using AE.PID.ViewModels;

namespace AE.PID.Views.Windows;

public class SecondaryWindow : WindowBase
{
    public SecondaryWindow(Window owner) : base(owner)
    {
        ShowInTaskbar = false;
        WindowButtonStyle = WindowButton.CloseOnly;
        WindowStartupLocation = WindowStartupLocation.Manual;

        // bind to owner size if user does not modify the current window
        Observable.FromEventPattern<SizeChangedEventHandler, SizeChangedEventArgs>(
                handler => owner.SizeChanged += handler,
                handler => owner.SizeChanged -= handler
            )
            .Select(_ => Unit.Default)
            .Merge(Observable.FromEventPattern<EventHandler, System.EventArgs>(
                    handler => owner.LocationChanged += handler,
                    handler => owner.LocationChanged -= handler
                )
                .Select(_ => Unit.Default))
            .Subscribe(_ =>
            {
                if (DataContext is WindowViewModel { IsResizedOrMoved: true }) return;

                Height = owner.ActualHeight;
                Top = owner.Top;
                Left = owner.Left + owner.Width;
            });
    }
}