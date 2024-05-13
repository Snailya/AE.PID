using System.ComponentModel;
using AE.PID.ViewModels;

namespace AE.PID.Views.Windows;

/// <summary>
///     Interaction logic for ChildWindow.xaml
/// </summary>
public partial class ChildWindow
{
    public ChildWindow()
    {
        InitializeComponent();

        DataContext = new WindowViewModel(this);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        Hide();
        e.Cancel = true;
    }
}