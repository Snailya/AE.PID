using AE.PID.Visio.UI.Avalonia.ViewModels;
using AE.PID.Visio.UI.Avalonia.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace AE.PID.Visio.UI.Avalonia;

public class AvaloniaApp : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new ProjectExplorerWindow
            {
                DataContext = ViewModelLocator.Create<ProjectExplorerWindowViewModel>()
            };

        base.OnFrameworkInitializationCompleted();
    }
}