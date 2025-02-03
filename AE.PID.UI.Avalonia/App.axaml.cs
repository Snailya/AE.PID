using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

namespace AE.PID.Client.UI.Avalonia;

public class AvaloniaApp : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new Window
            {
                Width = 0,
                Height = 0,
                ShowInTaskbar = false,
                IsVisible = false,
                SystemDecorations= SystemDecorations.None,
                Name = "AvaloniaHostWindow"
            };

        base.OnFrameworkInitializationCompleted();
    }
}