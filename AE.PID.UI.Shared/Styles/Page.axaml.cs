using Avalonia;
using Avalonia.Controls;

namespace AE.PID.UI.Shared;

public class Page : ContentControl
{
    public static readonly StyledProperty<bool> IsLoadingProperty = AvaloniaProperty.Register<Page, bool>(
        nameof(IsLoading));

    public bool IsLoading
    {
        get => GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }
}