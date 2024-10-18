using System;
using AE.PID.Visio.UI.Avalonia.ViewModels;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.Views;

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