using Avalonia.ReactiveUI;

namespace AE.PID.UI.Shared;

public partial class PageNavigator : ReactiveUserControl<PageNavigatorViewModel>
{
    public PageNavigator()
    {
        InitializeComponent();
    }
}