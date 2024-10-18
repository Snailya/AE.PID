using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Splat;

namespace AE.PID.Visio.Services;

public sealed class QueuedBackgroundService(BackgroundTaskQueue taskQueue) : BackgroundService, IEnableLogger
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
            try
            {
                var workItem =
                    await taskQueue.DequeueAsync(stoppingToken);

                await workItem(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if stoppingToken was signaled
            }
            catch (Exception ex)
            {
                this.Log().Error(ex, "Error occurred executing task work item.");
            }
    }
}