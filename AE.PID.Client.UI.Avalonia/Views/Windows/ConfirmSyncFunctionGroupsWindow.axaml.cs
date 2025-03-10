using System;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public partial class ConfirmSyncFunctionGroupsWindow : ReactiveWindow<ConfirmSyncFunctionGroupsViewModel>
{
    public ConfirmSyncFunctionGroupsWindow()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            d(ViewModel!.Confirm.Subscribe(_ => Close()));
            d(ViewModel!.Cancel.Subscribe(_ => Close()));
        });
    }
}