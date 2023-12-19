using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using AE.PID.ViewModels;
using ReactiveUI;

namespace AE.PID.Views;

/// <summary>
///     UserSettingsView.xaml 的交互逻辑
/// </summary>
public partial class TaskProgressView
{
    public TaskProgressView(TaskProgressViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;

        this.WhenActivated(disposableRegistration =>
        {
            this.OneWayBind(ViewModel, vm => vm.Current, v => v.ProgressBar.Value).DisposeWith(disposableRegistration);
            this.BindCommand(ViewModel, vm => vm.Cancel, v => v.CancelButton).DisposeWith(disposableRegistration);
        });

        this.WhenAnyObservable(x => x.ViewModel.Cancel).Subscribe(_ => Close());
        this.WhenAnyValue(x => x.ViewModel.Current).Where(x => x >= 100).Subscribe(_ => Close());
    }

    private void Close()
    {
        var window = Window.GetWindow(this);
        if (window != null) window.Visibility = Visibility.Collapsed;
    }
}