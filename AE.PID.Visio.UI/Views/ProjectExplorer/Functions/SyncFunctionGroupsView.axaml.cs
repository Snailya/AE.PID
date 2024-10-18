using AE.PID.Visio.UI.Avalonia.ViewModels;
using Avalonia.ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.Views;

public partial class SyncFunctionGroupsView : ReactiveUserControl<ConfirmSyncFunctionGroupsViewModel>
{
    public SyncFunctionGroupsView()
    {
        InitializeComponent();
    }
}