using AE.PID.Visio.UI.Avalonia.ViewModels;
using Avalonia.ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.Views;

public partial class AccountSettingView : ReactiveUserControl<AccountSettingViewModel>
{
    public AccountSettingView()
    {
        InitializeComponent();
    }
}