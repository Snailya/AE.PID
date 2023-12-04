using AE.PID.ViewModels;
using ReactiveUI;

namespace AE.PID.Views;

/// <summary>
///     UserSettingsView.xaml 的交互逻辑
/// </summary>
public partial class UserSettingsView : ReactiveUserControl<UserSettingsViewModel>
{
    public UserSettingsView()
    {
        InitializeComponent();
    }
}