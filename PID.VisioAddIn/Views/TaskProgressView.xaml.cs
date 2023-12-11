using System.Threading;
using AE.PID.ViewModels;
using ReactiveUI;

namespace AE.PID.Views;

/// <summary>
///     UserSettingsView.xaml 的交互逻辑
/// </summary>
public partial class TaskProgressView : ReactiveUserControl<TaskProgressViewModel>
{
    public TaskProgressView(CancellationTokenSource cts)
    {
        InitializeComponent();
        ViewModel = new TaskProgressViewModel(cts);

        this.WhenActivated(disposableRegistration =>
        {
            this.BindCommand(ViewModel, vm => vm.Cancel, v => v.CancelButton);
        });
    }
}