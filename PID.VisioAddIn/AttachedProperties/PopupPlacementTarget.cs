using System.Windows;
using System.Windows.Controls.Primitives;

namespace AE.PID.AttachedProperties;

public class PopupPlacementTarget : AttachedPropertyBase<PopupPlacementTarget, DependencyObject>
{
    /// <summary>
    ///     Gets popup control and modify it's position.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public override void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not DependencyObject target || sender is not Popup popup) return;

        var window = Window.GetWindow(target);
        if (null != window)
            window.LocationChanged += delegate
            {
                var horizontalOffset = popup.HorizontalOffset;
                popup.HorizontalOffset = horizontalOffset + 1;
                popup.HorizontalOffset = horizontalOffset;
            };
    }
}