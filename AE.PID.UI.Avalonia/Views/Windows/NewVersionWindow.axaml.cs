using System;
using AE.PID.UI.Avalonia.ViewModels;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace AE.PID.UI.Avalonia.Views;

public partial class NewVersionWindow : ReactiveWindow<NewVersionViewModel>
{
    public NewVersionWindow()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            d(ViewModel!.Confirm.Subscribe(_ => Close()));
            d(ViewModel!.Cancel.Subscribe(_ => Close()));
        });
    }
}