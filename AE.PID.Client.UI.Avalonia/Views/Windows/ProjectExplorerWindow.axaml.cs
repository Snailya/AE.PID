using System.Reactive;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using AE.PID.Client.UI.Avalonia.Shared;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Platform.Storage;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public partial class ProjectExplorerWindow : WindowBase<ProjectExplorerWindowViewModel>
{
    public ProjectExplorerWindow()
    {
        InitializeComponent();

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

    private static FilePickerFileType Workbook { get; } = new("")
    {
        Patterns = ["*.xlsx"]
    };


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
        IInteractionContext<SelectMaterialWindowViewModel?, MaterialViewModel?> interaction)
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

    private async Task DoShowSaveFilePickerAsync(IInteractionContext<string, IStorageFile?> interaction)
    {
        // 2025.2.10：新增默认的文件后缀
        var result = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "保存文件",
            DefaultExtension = interaction.Input,
            FileTypeChoices = [Workbook]
        });
        interaction.SetOutput(result);
    }

    #endregion
}