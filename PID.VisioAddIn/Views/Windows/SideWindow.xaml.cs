using System.ComponentModel;
using System.Windows;

namespace AE.PID.Views.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class SideWindow
{
    public SideWindow()
    {
        MaxWidth = SystemParameters.WorkArea.Size.Width;
        MaxHeight = SystemParameters.WorkArea.Size.Height;

        InitializeComponent();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        Hide();
        e.Cancel = true;
    }
}