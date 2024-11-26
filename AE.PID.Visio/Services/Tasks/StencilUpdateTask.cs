using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AE.PID.Visio.Core.Interfaces;
using Microsoft.Office.Interop.Visio;
using Splat;

namespace AE.PID.Visio.Services.Tasks;

public class StencilUpdateTask(
    IConfigurationService configurationService,
    StencilUpdateService stencilUpdateService,
    Application application) : BackgroundTaskBase, IEnableLogger
{
    public override string TaskName { get; } = "Stencil Update Task";

    public override async Task ExecuteAsync(CancellationToken cts)
    {
        await base.ExecuteAsync(cts);

        var stencilsInConfiguration =
            configurationService.GetCurrentConfiguration().Stencils.ToList();
        var openedDocuments = application.Documents.OfType<Document>()
            .Where(x => stencilsInConfiguration.Any(i => i.Name == x.Name))
            .ToList();
        var openedDocumentNames = openedDocuments.Select(x => x.Name).ToList();

        // close the opened stencils
        this.Log().Info("Close the opened documents before doing stencil update.");

        foreach (var document in openedDocuments) document.Close();

        try
        {
            var updated = (await stencilUpdateService.UpdateAsync()).ToList();

            this.Log().Info("Update stencils successfully.");

            // restore with updated files
            foreach (var toOpen in updated.Where(x => openedDocuments.Any(i => i.Name == x.Name)))
                application.Documents.AddEx(toOpen.FilePath);
        }
        catch (Exception e)
        {
            this.Log().Error(e, "Update stencils failed, restore from previous configuration");

            // restore from the opened
            foreach (var toOpen in openedDocumentNames)
                if (stencilsInConfiguration.SingleOrDefault(x => x.Name == toOpen) is { } config)
                    application.Documents.AddEx(config.FilePath);
        }
    }
}