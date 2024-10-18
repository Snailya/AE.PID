using System;
using AE.PID.Visio.UI.Avalonia.ViewModels;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.Views;

public partial class ConfirmSyncMaterialsWindow : ReactiveWindow<SyncMaterialsViewModel>
{
    public ConfirmSyncMaterialsWindow()
    {
        InitializeComponent();
        this.WhenActivated(d => d(ViewModel!.Confirm.Subscribe(v => Close(v))));
    }
}