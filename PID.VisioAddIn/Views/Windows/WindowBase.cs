using System.ComponentModel;
using System.Windows;
using AE.PID.ViewModels;

namespace AE.PID.Views.Windows;

public abstract class WindowBase : Window
{
    protected WindowBase()
    {
        MaxHeight = SystemParameters.WorkArea.Height;
        MaxWidth = SystemParameters.WorkArea.Width;

        DataContext = new WindowViewModel(this);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        foreach (Window ownedWindow in OwnedWindows) ownedWindow.Close();

        Hide();
        Content = null;

        e.Cancel = true;
    }
}