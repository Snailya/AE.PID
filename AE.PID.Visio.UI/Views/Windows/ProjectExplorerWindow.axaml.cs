using System.Reactive;
using System.Threading.Tasks;
using AE.PID.Visio.Core.Models;
using AE.PID.Visio.UI.Avalonia.ViewModels;
using Avalonia;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Platform.Storage;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.Views;

public partial class ProjectExplorerWindow : WindowBase<ProjectExplorerWindowViewModel>
{
    public ProjectExplorerWindow()
    {
        InitializeComponent();

#if DEBUG
        this.AttachDevTools();
#endif
        this.WhenActivated(action =>
            {
                // set file service
                action(ViewModel!.Projects.ShowSelectProjectDialog.RegisterHandler(DoShowSelectProjectDialogAsync));

                action(ViewModel!.Functions.Kanban.ShowSelectFunctionDialog.RegisterHandler(
                    DoShowSelectFunctionZoneDialogAsync));
                action(ViewModel!.Functions.Kanban.ShowSyncFunctionGroupsDialog.RegisterHandler(
                    DoShowConfirmSyncFunctionGroupsDialogAsync));

                action(ViewModel!.Materials.ShowSelectMaterialDialog.RegisterHandler(DoShowSelectMaterialDialogAsync));
                action(ViewModel!.Materials.ShowSyncMaterialsDialog.RegisterHandler(DoShowSyncMaterialsDialogAsync));
                action(ViewModel!.Materials.SaveFilePicker.RegisterHandler(DoShowSaveFilePickerAsync));
            }
        );
    }


    #region -- Projects --

    private async Task DoShowSelectProjectDialogAsync(
        IInteractionContext<SelectProjectViewModel?, ProjectViewModel?> interaction)
    {
        var dialog = new SelectProjectWindow
        {
            DataContext = interaction.Input
        };

        var result = await dialog.ShowDialog<ProjectViewModel?>(this);
        interaction.SetOutput(result);
    }

    #endregion

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        ViewModel!.NotificationManager =
            new WindowNotificationManager(GetTopLevel(this)!)
            {
                // Margin = new Thickness(32)
            };
    }

    #region -- Functions --

    private async Task DoShowSelectFunctionZoneDialogAsync(
        IInteractionContext<SelectFunctionViewModel?, FunctionViewModel?> interaction)
    {
        var dialog = new SelectFunctionZoneWindow
        {
            DataContext = interaction.Input
        };

        var result = await dialog.ShowDialog<FunctionViewModel?>(this);
        interaction.SetOutput(result);
    }

    private async Task DoShowConfirmSyncFunctionGroupsDialogAsync(
        IInteractionContext<ConfirmSyncFunctionGroupsViewModel?, Function[]?> interaction)
    {
        var dialog = new ConfirmSyncFunctionGroupsWindow
        {
            DataContext = interaction.Input
        };

        var result = await dialog.ShowDialog<Function[]?>(this);
        interaction.SetOutput(result);
    }

    #endregion

    #region -- Materials --

    private async Task DoShowSelectMaterialDialogAsync(
        IInteractionContext<SelectMaterialViewModel?, MaterialViewModel?> interaction)
    {
        var dialog = new SelectMaterialWindow
        {
            DataContext = interaction.Input
        };

        var result = await dialog.ShowDialog<MaterialViewModel?>(this);
        interaction.SetOutput(result);
    }

    private async Task DoShowSyncMaterialsDialogAsync(IInteractionContext<SyncMaterialsViewModel, Unit> interaction)
    {
        var dialog = new ConfirmSyncMaterialsWindow
        {
            DataContext = interaction.Input
        };

        var result = await dialog.ShowDialog<Unit>(this);
        interaction.SetOutput(result);
    }

    private async Task DoShowSaveFilePickerAsync(IInteractionContext<Unit, IStorageFile?> interaction)
    {
        var result = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Text File"
        });
        interaction.SetOutput(result);
    }

    #endregion
}