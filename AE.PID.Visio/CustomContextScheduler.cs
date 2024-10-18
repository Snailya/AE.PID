using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;

namespace AE.PID.Visio;

public class CustomContextScheduler : IScheduler
{
    private readonly SynchronizationContext _mainContext;

    public CustomContextScheduler(SynchronizationContext mainContext)
    {
        _mainContext = mainContext;
    }

    public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
    {
        // Post to SynchronizationContext, ensuring it's done on the VSTO main thread
        var disposable = new CancellationDisposable();

        _mainContext.Post(_ =>
        {
            if (!disposable.IsDisposed) action(this, state);
        }, null);

        return disposable;
    }

    public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime,
        Func<IScheduler, TState, IDisposable> action)
    {
        var delay = dueTime - Now;
        return Schedule(state, delay, action);
    }

    public IDisposable Schedule<TState>(TState state, TimeSpan dueTime,
        Func<IScheduler, TState, IDisposable> action)
    {
        var disposable = new CancellationDisposable();

        Timer timer = null;
        timer = new Timer(_ =>
        {
            if (!disposable.IsDisposed)
                _mainContext.Post(__ =>
                {
                    if (!disposable.IsDisposed) action(this, state);
                }, null);

            timer.Dispose();
        }, null, dueTime, TimeSpan.FromMilliseconds(-1));

        return disposable;
    }

    public DateTimeOffset Now => DateTimeOffset.Now;
}