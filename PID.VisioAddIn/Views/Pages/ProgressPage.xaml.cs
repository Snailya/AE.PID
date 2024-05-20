using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Services;
using AE.PID.ViewModels;
using ReactiveUI;

namespace AE.PID.Views;

/// <summary>
///     Interaction logic for MockPage.xaml
/// </summary>
public partial class ProgressPage
{
    public ProgressPage(ProgressPageViewModel progressViewModel)
    {
        InitializeComponent();

        ViewModel = progressViewModel;

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.IsIndeterminate, v => v.ProgressBar.IsIndeterminate).DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.ProgressValue!.Value, v => v.ProgressBar.Value).DisposeWith(d);
            this.OneWayBind(ViewModel, vm => vm.ProgressValue!.Message, v => v.Message.Text).DisposeWith(d);

            this.WhenAnyValue(x => x.ViewModel!.ProgressValue!.Status).Where(x => x == TaskStatus.RanToCompletion)
                .Subscribe(_ => Close())
                .DisposeWith(d);
        });
    }
}