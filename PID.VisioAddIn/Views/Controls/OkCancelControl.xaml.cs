using System;
using System.Reactive.Disposables;
using ReactiveUI;

namespace AE.PID.Views.Controls;

/// <summary>
/// Interaction logic for OkCancelControl.xaml
/// </summary>
public partial class OkCancelControl
{
    public OkCancelControl()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.BindCommand(ViewModel, vm => vm.Ok, v => v.OkButton)
                .DisposeWith(d);
            this.BindCommand(ViewModel, vm => vm.Cancel, v => v.CancelButton)
                .DisposeWith(d);

            this.WhenAnyObservable(x => x.ViewModel.Ok, x => x.ViewModel.Cancel)
                .Subscribe(_ => Close())
                .DisposeWith(d);
        });
    }
}