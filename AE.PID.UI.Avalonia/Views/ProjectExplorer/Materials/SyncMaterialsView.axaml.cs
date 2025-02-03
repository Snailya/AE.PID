using AE.PID.Client.UI.Avalonia;
using AE.PID.UI.Avalonia.ViewModels;
using Avalonia.ReactiveUI;

namespace AE.PID.UI.Avalonia.Views;

/// <summary>
///     Interaction logic for MockPage.xaml
/// </summary>
public partial class SyncMaterialsView : ReactiveUserControl<SyncMaterialsViewModel>
{
    public SyncMaterialsView()
    {
        InitializeComponent();
    }
}