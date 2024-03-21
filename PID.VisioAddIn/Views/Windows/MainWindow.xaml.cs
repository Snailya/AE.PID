using System.ComponentModel;
using System.Windows;

namespace AE.PID.Views.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        MaxWidth = SystemParameters.WorkArea.Size.Width;
        MaxHeight = SystemParameters.WorkArea.Size.Height;

        InitializeComponent();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        foreach (Window ownedWindow in OwnedWindows) ownedWindow.Close();

        Hide();
        e.Cancel = true;
    }
}