using System;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public partial class NewVersionWindow : ReactiveWindow<NewVersionWindowViewModel>
{
    public NewVersionWindow()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            d(ViewModel!.Confirm.Subscribe(_ => Close(true)));
            d(ViewModel!.Cancel.Subscribe(_ => Close(false)));
        });
    }
}