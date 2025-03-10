using System;
using AE.PID.Client.UI.Avalonia.Shared;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public partial class SelectProjectWindow : WindowBase<SelectProjectViewModel>
{
    public SelectProjectWindow()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            d(ViewModel!.Confirm.Subscribe(Close));
            d(ViewModel!.Cancel.Subscribe(_ => Close()));
        });
    }
}