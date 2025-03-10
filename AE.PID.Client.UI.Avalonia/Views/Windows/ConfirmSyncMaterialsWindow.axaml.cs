using System;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public partial class ConfirmSyncMaterialsWindow : ReactiveWindow<SyncMaterialsViewModel>
{
    public ConfirmSyncMaterialsWindow()
    {
        InitializeComponent();
        this.WhenActivated(d => d(ViewModel!.Confirm.Subscribe(v => Close(v))));
    }
}