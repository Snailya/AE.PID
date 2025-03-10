using Avalonia;
using Avalonia.Controls.Primitives;

namespace AE.PID.Client.UI.Avalonia.Shared;

public class LoadingIndicator : TemplatedControl
{
    public static readonly StyledProperty<bool> IsLoadingProperty = AvaloniaProperty.Register<LoadingIndicator, bool>(
        nameof(IsLoading));

    public bool IsLoading
    {
        get => GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }
}