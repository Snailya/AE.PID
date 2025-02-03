using System;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace AE.PID.UI.Avalonia.Views;

public partial class SelectFunctionZoneWindow : ReactiveWindow<SelectFunctionViewModel>
{
    public SelectFunctionZoneWindow()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            d(ViewModel!.Confirm.Subscribe(Close));
            d(ViewModel!.Cancel.Subscribe(_ => Close()));
        });
    }
}