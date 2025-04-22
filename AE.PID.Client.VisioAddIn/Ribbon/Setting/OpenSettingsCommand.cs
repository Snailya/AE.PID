using AE.PID.Client.Core;
using AE.PID.Client.UI.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Office.Core;

namespace AE.PID.Client.VisioAddIn.Setting;

internal class OpenSettingsCommand : RibbonCommandBase
{
    public override string Id { get; } = nameof(OpenSettingsCommand);

    public override void Execute(IRibbonControl control)
    {
        var vm = ThisAddIn.Services.GetRequiredService<SettingsWindowViewModel>();
        var ui = ThisAddIn.Services.GetRequiredService<IUserInteractionService>();

        ui.Show(vm, ThisAddIn.GetApplicationHandle());
    }

    public override string GetLabel(IRibbonControl control)
    {
        return "设置";
    }
}