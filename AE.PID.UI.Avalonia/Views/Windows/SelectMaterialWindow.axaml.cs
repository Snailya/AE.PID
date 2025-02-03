using System;
using AE.PID.Client.UI.Avalonia;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace AE.PID.UI.Avalonia.Views;

public partial class SelectMaterialWindow : ReactiveWindow<SelectMaterialWindowViewModel>
{
    public SelectMaterialWindow()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            d(ViewModel!.Confirm.Subscribe(Close));
            d(ViewModel!.Cancel.Subscribe(_ => Close()));
        });
    }
}