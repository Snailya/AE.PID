using AE.PID.Visio.UI.Avalonia.ViewModels;
using Avalonia.ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.Views;

public partial class PageNavigator : ReactiveUserControl<PageNavigatorViewModel>
{
    public PageNavigator()
    {
        InitializeComponent();
    }
}