using AE.PID.UI.Avalonia;
using Avalonia.ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public partial class AccountSettingView : ReactiveUserControl<AccountSettingViewModel>
{
    public AccountSettingView()
    {
        InitializeComponent();
    }
}