using System;
using System.Reactive.Disposables;
using Splat;

namespace AE.PID.Services;

public abstract class ServiceBase : IDisposable, IEnableLogger
{
    protected readonly CompositeDisposable CleanUp = new();

    public void Dispose()
    {
        Stop();
    }

    public abstract void Start();

    public void Stop()
    {
        CleanUp.Dispose();
    }
}