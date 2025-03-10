using System;
using System.Threading;
using System.Threading.Tasks;

namespace AE.PID.Client.VisioAddIn;

public abstract class BackgroundTaskBase : IBackgroundTask
{
    private int _retryCount;

    public abstract string TaskName { get; }
    public bool ShouldRetry => _retryCount < MaxRetryAttempts;
    public TimeSpan RetryDelay { get; } = TimeSpan.FromMinutes(15);
    public int MaxRetryAttempts { get; set; } = 3;

    public virtual Task ExecuteAsync(CancellationToken cts)
    {
        _retryCount++;

        return Task.CompletedTask;
    }
}