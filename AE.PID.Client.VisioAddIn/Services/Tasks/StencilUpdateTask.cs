using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using Microsoft.Office.Interop.Visio;
using Splat;

namespace AE.PID.Client.VisioAddIn;

internal class StencilUpdateTask(
    IConfigurationService configurationService,
    StencilUpdateService stencilUpdateService,
    Application application) : BackgroundTaskBase, IEnableLogger
{
    public override string TaskName { get; } = "Stencil Update Task";

    public override async Task ExecuteAsync(CancellationToken cts)
    {
        await base.ExecuteAsync(cts);

        var configurationStencils =
            configurationService.GetCurrentConfiguration().Stencils.ToList();

        var openedStencils = application.Documents
            .OfType<Document>()
            .Select(x => new Stencil(x.Name, x.FullName))
            .Where(x => configurationStencils.Any(i => i.Name == x.Name))
            .ToList();

        // close the opened stencils
        this.Log().Info("Close the opened documents before doing stencil update.");

        foreach (var stencil in openedStencils) application.Documents[stencil.Name].Close();

        try
        {
            _ = (await stencilUpdateService.UpdateAsync()).ToList();

            this.Log().Info("Update stencils successfully.");
        }
        catch (Exception e)
        {
            this.Log().Error(e, "Update stencils failed, restore from previous configuration");

            throw new StencilFailedToUpdateException("Update stencils failed, restore from previous configuration");
        }
        finally
        {
            // restore from the opened
            foreach (var toOpen in openedStencils)
                if (configurationStencils.SingleOrDefault(x => x.Name == toOpen.Name) is { } config)
                    application.Documents.AddEx(config.FilePath);
        }
    }

    private class Stencil(string name, string filePath)
    {
        public string Name { get; } = name;
        public string FilePath { get; set; } = filePath;
    }
}

public class StencilFailedToUpdateException(string message) : Exception(message);