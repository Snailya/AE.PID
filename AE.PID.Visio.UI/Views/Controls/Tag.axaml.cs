using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;

namespace AE.PID.Visio.UI.Avalonia.Views;

public class Tag : ContentControl
{
    public static readonly StyledProperty<object> CloseIconProperty = AvaloniaProperty.Register<Tag, object>(
        nameof(CloseIcon));

    public static readonly StyledProperty<IBrush> ColorProperty =
        AvaloniaProperty.Register<Tag, IBrush>(
            nameof(Color));

    public static readonly StyledProperty<object> IconProperty = AvaloniaProperty.Register<Tag, object>(
        nameof(Icon));

    public static readonly StyledProperty<ICommand?> OnCloseProperty = AvaloniaProperty.Register<Tag, ICommand?>(
        nameof(OnClose));

    public object CloseIcon
    {
        get => GetValue(CloseIconProperty);
        set => SetValue(CloseIconProperty, value);
    }

    public IBrush Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public object Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public ICommand? OnClose
    {
        get => GetValue(OnCloseProperty);
        set => SetValue(OnCloseProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        
        var closeButton = e.NameScope.Find<ContentControl>("PART_CloseIcon");
        if (closeButton != null) closeButton.PointerPressed += RaiseOnClose;
    }

    private void RaiseOnClose(object sender, PointerPressedEventArgs e)
    {
        if (e.Handled || OnClose is null || !OnClose.CanExecute(DataContext)) return;
        
        OnClose.Execute(DataContext);
        e.Handled = true;
    }
}