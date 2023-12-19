using System.ComponentModel;
using System.Windows;

namespace AE.PID.Views;

public class MainWindow : Window
{
    protected override void OnClosing(CancelEventArgs e)
    {
        Visibility = Visibility.Collapsed;
        e.Cancel = true;
    }
}