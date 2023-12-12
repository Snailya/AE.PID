using AE.PID.ViewModels;
using ReactiveUI;

namespace AE.PID.Views;

/// <summary>
///     UserSettingsView.xaml 的交互逻辑
/// </summary>
public partial class TaskProgressView : ReactiveUserControl<TaskProgressViewModel>
{
    public TaskProgressView(TaskProgressViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;

        this.WhenActivated(disposableRegistration =>
        {
            this.OneWayBind(ViewModel, vm => vm.Current, v => v.ProgressBar.Value);
            this.BindCommand(ViewModel, vm => vm.Cancel, v => v.CancelButton);
        });
    }
}