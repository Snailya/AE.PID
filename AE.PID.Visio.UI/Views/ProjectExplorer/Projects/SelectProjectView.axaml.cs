using AE.PID.Visio.UI.Avalonia.ViewModels;
using Avalonia.ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.Views;

/// <summary>
///     Interaction logic for MockPage.xaml
/// </summary>
public partial class SelectProjectView : ReactiveUserControl<SelectProjectViewModel>
{
    public SelectProjectView()
    {
        InitializeComponent();
    }
}