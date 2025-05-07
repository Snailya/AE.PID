using System;
using AE.PID.Client.Core;
using AE.PID.Client.UI.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Office.Core;

namespace AE.PID.Client.VisioAddIn;

internal sealed class OpenProjectExplorerCommand : RibbonCommandBase
{
    public override string Id { get; } = nameof(OpenProjectExplorerCommand);

    public override void Execute(IRibbonControl control)
    {
        var document = Globals.ThisAddIn.Application.ActiveDocument;
        var scope = ThisAddIn.ServiceBridge.GetScope(document);

        var vm = scope.ServiceProvider.GetRequiredService<ProjectExplorerWindowViewModel>();
        var ui = scope.ServiceProvider.GetRequiredService<IUserInteractionService>();

        ui.Show(vm, new IntPtr(Globals.ThisAddIn.Application.WindowHandle32),
            () => { ThisAddIn.ServiceBridge.ReleaseScope(document); });
    }

    public override bool CanExecute(IRibbonControl control)
    {
        return IsPageWindow();
    }

    public override string GetLabel(IRibbonControl control)
    {
        return "项目浏览器";
    }
}