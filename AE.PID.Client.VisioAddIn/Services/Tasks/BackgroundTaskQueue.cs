using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AE.PID.Client.VisioAddIn;

public sealed class BackgroundTaskQueue
{
    private readonly Channel<IBackgroundTask> _queue;

    public BackgroundTaskQueue(int capacity = 100)
    {
        BoundedChannelOptions options = new(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<IBackgroundTask>(options);
    }

    public async ValueTask QueueBackgroundTaskAsync(
        IBackgroundTask task, CancellationToken cts)
    {
        if (task is null)
            throw new ArgumentNullException(nameof(task));

        await _queue.Writer.WriteAsync(task, cts);
    }

    public async ValueTask<IBackgroundTask> DequeueAsync(
        CancellationToken cts)
    {
        var task =
            await _queue.Reader.ReadAsync(cts);
        return task;
    }
}