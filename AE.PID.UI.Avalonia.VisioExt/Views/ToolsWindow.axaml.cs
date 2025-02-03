using Avalonia;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace AE.PID.UI.Avalonia.VisioExt;

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