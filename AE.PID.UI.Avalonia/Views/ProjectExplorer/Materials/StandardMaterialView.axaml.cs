using AE.PID.Client.UI.Avalonia;
using Avalonia.ReactiveUI;

namespace AE.PID.UI.Avalonia.Views;

/// <summary>
///     Interaction logic for MockPage.xaml
/// </summary>
public partial class StandardMaterialView : ReactiveUserControl<StandardMaterialViewModel>
{
    public StandardMaterialView()
    {
        InitializeComponent();
    }
}