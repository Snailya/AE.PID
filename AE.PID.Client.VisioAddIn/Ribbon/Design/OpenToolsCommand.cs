using AE.PID.Client.Core;
using AE.PID.Client.UI.Avalonia.VisioExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Office.Core;

namespace AE.PID.Client.VisioAddIn;

internal sealed class OpenToolsCommand : RibbonCommandBase
{
    public override string Id { get; } = nameof(OpenToolsCommand);

    public override void Execute(IRibbonControl control)
    {
        var document = Globals.ThisAddIn.Application.ActiveDocument;
        var scope = ThisAddIn.ScopeManager.GetScope(document);

        var vm = scope.ServiceProvider.GetRequiredService<ToolsWindowViewModel>();
        var ui = scope.ServiceProvider.GetRequiredService<IUserInteractionService>();

        ui.Show(vm, ThisAddIn.GetApplicationHandle(),
            () => { ThisAddIn.ScopeManager.ReleaseScope(document); });
    }

    public override bool CanExecute(IRibbonControl control)
    {
        return IsPageWindow();
    }

    public override string GetLabel(IRibbonControl control)
    {
        return "工具";
    }
}