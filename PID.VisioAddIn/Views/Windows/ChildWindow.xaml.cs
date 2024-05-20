using AE.PID.ViewModels;

namespace AE.PID.Views.Windows;

/// <summary>
///     Interaction logic for ChildWindow.xaml
/// </summary>
public partial class ChildWindow : WindowBase
{
    public ChildWindow()
    {
        InitializeComponent();

        DataContext = new WindowViewModel(this);
    }
}