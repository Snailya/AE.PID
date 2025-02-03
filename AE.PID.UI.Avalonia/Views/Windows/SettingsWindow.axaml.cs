using System.Threading.Tasks;
using AE.PID.UI.Avalonia.ViewModels;
using Avalonia;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace AE.PID.UI.Avalonia.Views;

public partial class SettingsWindow : ReactiveWindow<SettingsWindowViewModel>
{
    public SettingsWindow()
    {
        InitializeComponent();

#if DEBUG
        this.AttachDevTools();
#endif

        this.WhenActivated(action =>
            {
                action(ViewModel!.About.ShowNewVersionView.RegisterHandler(DoShowNewVersionDialogAsync));
            }
        );
    }

    private async Task DoShowNewVersionDialogAsync(
        IInteractionContext<NewVersionViewModel, bool> interaction)
    {
        var dialog = new NewVersionWindow
        {
            DataContext = interaction.Input
        };

        var result = await dialog.ShowDialog<bool>(this);
        interaction.SetOutput(result);
    }
}