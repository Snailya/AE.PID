using System.ComponentModel;

namespace AE.PID.Views.Windows;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class SideWindow
{
    public SideWindow()
    {
        InitializeComponent();

        DataContext = new BaseWindowViewModel(this);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        Hide();
        e.Cancel = true;
    }
}