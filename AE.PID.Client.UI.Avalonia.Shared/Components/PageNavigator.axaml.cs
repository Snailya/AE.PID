using Avalonia.ReactiveUI;

namespace AE.PID.Client.UI.Avalonia.Shared;

public partial class PageNavigator : ReactiveUserControl<PageNavigatorViewModel>
{
    public PageNavigator()
    {
        InitializeComponent();
    }
}