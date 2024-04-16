using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace AE.PID.Views.Windows;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        MaxHeight = SystemParameters.WorkArea.Height;
        MaxWidth = SystemParameters.WorkArea.Width;

        // bind view partModel
        DataContext = new BaseWindowViewModel(this);

        Loaded += (sender, e) => { SizeToContent = SizeToContent.Manual; };
    }


    protected override void OnClosing(CancelEventArgs e)
    {
        foreach (Window ownedWindow in OwnedWindows) ownedWindow.Close();

        Hide();
        e.Cancel = true;

        SizeToContent = SizeToContent.WidthAndHeight;
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is Button button)
            switch (button.Name)
            {
                case "PART_Minimize":
                    WindowState = WindowState.Minimized;
                    break;
                case "PART_Maximize":
                    WindowState = WindowState.Maximized;
                    break;
                case "PART_Close":
                    Close();
                    break;
            }
    }
}