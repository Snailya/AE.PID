using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Splat;

namespace AE.PID.Client.VisioAddIn;

public class BackgroundTaskExecutor(BackgroundTaskQueue taskQueue) : BackgroundService, IEnableLogger
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IBackgroundTask? task = null;
            try
            {
                task =
                    await taskQueue.DequeueAsync(stoppingToken);

                this.Log().Debug($"Executing {task.TaskName}...");
                await task.ExecuteAsync(stoppingToken);
                this.Log().Info($"{task.TaskName} completed successfully.");
            }
            catch (OperationCanceledException ex)
            {
                if (task != null)
                    this.Log().Warn($"Task {task.TaskName} cancelled.", ex);
            }
            catch (Exception e)
            {
                this.Log().Error(e, $"{task!.TaskName} failed.");

                if (task.ShouldRetry)
                {
                    var retryDelay = task.RetryDelay;
                    this.Log().Info($"{task.TaskName} will be retried after {retryDelay.TotalMinutes} minutes...");

                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(retryDelay, stoppingToken);

                        this.Log().Info($"Retrying {task.TaskName}...");
                        await taskQueue.QueueBackgroundTaskAsync(task, stoppingToken);
                    }, stoppingToken);
                }
            }
        }
    }
}