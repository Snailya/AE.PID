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
            var task =
                await taskQueue.DequeueAsync(stoppingToken);
            try
            {
                this.Log().Info($"Executing {task.TaskName}...");
                await task.ExecuteAsync(stoppingToken);
                this.Log().Info($"{task.TaskName} completed successfully.");
            }
            catch (Exception e)
            {
                this.Log().Error(e, $"{task.TaskName} failed.");

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