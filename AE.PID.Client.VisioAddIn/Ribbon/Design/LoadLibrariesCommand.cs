using AE.PID.Client.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Office.Core;
using Path = System.IO.Path;

namespace AE.PID.Client.VisioAddIn;

internal sealed class LoadLibrariesCommand : RibbonCommandBase
{
    public override string Id { get; } = nameof(LoadLibrariesCommand);

    public override void Execute(IRibbonControl control)
    {
        // 2025.02.07: 使用RuntimePath
        var service = ThisAddIn.ServiceBridge.GetRequiredService<IConfigurationService>();
        LibraryHelper.OpenLibraries(Path.Combine(service.RuntimeConfiguration.DataPath, "libraries"));
    }

    public override bool CanExecute(IRibbonControl control)
    {
        return IsPageWindow();
    }


    public override string GetLabel(IRibbonControl control)
    {
        return "库";
    }
}