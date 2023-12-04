using System.Collections.Generic;
using System.Text.Json;
using AE.PID.ViewModels;
using ReactiveUI;

namespace AE.PID.Views;

/// <summary>
///     VersionUpdatePromptView.xaml 的交互逻辑
/// </summary>
public partial class VersionUpdatePromptView : ReactiveUserControl<VersionUpdatePromptViewModel>
{
    public VersionUpdatePromptView(IEnumerable<JsonElement> stencilObjects)
    {
        InitializeComponent();
        ViewModel = new VersionUpdatePromptViewModel(stencilObjects);

        this.WhenActivated(disposableRegistration =>
        {
            this.OneWayBind(ViewModel, viewModel => viewModel.Description, view => view.Description.Text);
            this.BindCommand(ViewModel, viewModel => viewModel.Update, view => view.UpdateButton);
            this.BindCommand(ViewModel, viewModel => viewModel.NotNow, view => view.NotNowButton);
        });
    }
}