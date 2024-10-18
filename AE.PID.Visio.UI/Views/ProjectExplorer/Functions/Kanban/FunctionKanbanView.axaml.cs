using AE.PID.Visio.UI.Avalonia.ViewModels;
using Avalonia.ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.Views;

public partial class FunctionKanbanView : ReactiveUserControl<FunctionKanbanViewModel>
{
    public FunctionKanbanView()
    {
        InitializeComponent();
    }
}