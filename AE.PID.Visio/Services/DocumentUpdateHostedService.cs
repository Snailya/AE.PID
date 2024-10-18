using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AE.PID.Core.DTOs;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Office.Interop.Visio;
using Splat;

namespace AE.PID.Visio.Services;

public class DocumentUpdateHostedService : IHostedService, IEnableLogger
{
    private readonly Application _application;

    //  todo: 这个canceltoken没用上，肯定有问题
    private readonly CancellationToken _cancellationToken;
    private readonly IDocumentUpdateService _documentUpdateService;

    private readonly BackgroundTaskQueue _taskQueue;

    public DocumentUpdateHostedService(IHostApplicationLifetime applicationLifetime, BackgroundTaskQueue taskQueue,
        IDocumentUpdateService documentUpdateService, Application application)
    {
        _taskQueue = taskQueue;
        _documentUpdateService = documentUpdateService;
        _cancellationToken = applicationLifetime.ApplicationStopping;
        _application = application;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.Log().Info("Starting document update hosted service.");

        //// enqueue the current documents
        //foreach (var document in _application.Documents.OfType<Document>()
        //             .Where(x => x.Type == VisDocumentTypes.visTypeDrawing))
        //    if (IsValidWorkItem(document))
        //        _taskQueue.QueueBackgroundWorkItemAsync(cts => BuildWorkItemAsync(document, cts));

        // execute whenever there is a new opened document
        _application.BeforeDocumentClose += ApplicationOnBeforeDocumentClose;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.Log().Info("Stopping document update hosted service.");

        _application.BeforeDocumentClose -= ApplicationOnBeforeDocumentClose;

        return Task.CompletedTask;
    }

    private void ApplicationOnBeforeDocumentClose(Document doc)
    {
        if (!IsValidWorkItem(doc)) return;

        // todo: remove hidden information to reduce size(not work as it is already closed)
        //doc.RemoveHiddenInformation((int)VisRemoveHiddenInfoItems.visRHIMasters);

        var filePath = doc.FullName;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        Update(filePath);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }

    public async Task Update(string filePath, bool openAfterDone = false)
    {
        await _taskQueue.QueueBackgroundWorkItemAsync(cts => BuildWorkItemAsync(filePath, openAfterDone, cts));
    }

    private bool IsValidWorkItem(Document doc)
    {
        // check if the document is the visDrawing, not the stencil or other type
        if (doc.Type != VisDocumentTypes.visTypeDrawing) return false;

        // check if the AE style exist, if the AE style exist, means this is a target drawing.
        if (doc.Styles.OfType<IVStyle>().SingleOrDefault(x => x.Name == FormatHelper.NormalStyleName) ==
            null) return false;

        // check if the version is out of date
        var masters = doc.Masters.OfType<IVMaster>().Select(x => new MasterSnapshotDto
        {
            BaseId = x.BaseID,
            UniqueId = x.UniqueID
        });
        return _documentUpdateService.HasUpdate(masters);
    }

    private async ValueTask BuildWorkItemAsync(string filePath, bool openAfterDone, CancellationToken token)
    {
        // Simulate three 5-second tasks to complete
        // for each enqueued work item

        var guid = Guid.NewGuid();

        this.Log().Info("Queued work item {Guid} is starting.", guid);

        while (!token.IsCancellationRequested)
        {
            try
            {
                await _documentUpdateService.UpdateAsync(filePath);

                if (openAfterDone) _application.Documents.Add(filePath);
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if the Delay is cancelled
            }

            this.Log().Info("Queued Background Task {Guid} is complete.", guid);
            return;
        }

        this.Log().Info("Queued Background Task {Guid} was cancelled.", guid);
    }
}