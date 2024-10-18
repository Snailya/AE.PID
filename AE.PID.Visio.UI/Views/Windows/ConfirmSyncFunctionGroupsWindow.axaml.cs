using System;
using AE.PID.Visio.UI.Avalonia.ViewModels;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.Views;

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