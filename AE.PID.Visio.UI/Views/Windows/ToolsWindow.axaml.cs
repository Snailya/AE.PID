using AE.PID.Visio.UI.Avalonia.ViewModels;
using Avalonia;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.Views;

public partial class ToolsWindow : ReactiveWindow<ToolsWindowViewModel>
{
    public ToolsWindow()
    {
        InitializeComponent();

#if DEBUG
        this.AttachDevTools();
#endif

        this.WhenActivated(action => { }
        );
    }
}