using System;
using AE.PID.Client.UI.Avalonia;
using AE.PID.UI.Shared;
using ReactiveUI;

namespace AE.PID.UI.Avalonia.Views;

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