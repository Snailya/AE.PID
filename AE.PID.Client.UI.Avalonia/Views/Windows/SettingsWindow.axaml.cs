using Avalonia;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public partial class SettingsWindow : ReactiveWindow<SettingsWindowViewModel>
{
    public SettingsWindow()
    {
        InitializeComponent();

#if DEBUG
        this.AttachDevTools();
#endif

        this.WhenActivated(action => { }
        );
    }
}