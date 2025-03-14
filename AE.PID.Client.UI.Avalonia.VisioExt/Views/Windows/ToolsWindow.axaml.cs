using System.Reactive.Disposables;
using Avalonia;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia.VisioExt;

public partial class ToolsWindow : ReactiveWindow<ToolsWindowViewModel>
{
    public ToolsWindow()
    {
        InitializeComponent();

#if DEBUG
        this.AttachDevTools();
#endif

        this.WhenActivated(action => { ViewModel!.SelectTool.Cancel.Subscribe(_ => Close(null)).DisposeWith(action); }
        );
    }
}