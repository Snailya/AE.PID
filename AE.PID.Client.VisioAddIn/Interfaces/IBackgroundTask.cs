using System;
using System.Threading;
using System.Threading.Tasks;

namespace AE.PID.Client.VisioAddIn;

public interface IBackgroundTask
{
    string TaskName { get; }
    bool ShouldRetry { get; }

    TimeSpan RetryDelay { get; }

    int MaxRetryAttempts { get; set; } // 最大重试次数

    Task ExecuteAsync(CancellationToken cts);
}