using System;
using System.Reactive.Disposables;
using Microsoft.Office.Interop.Visio;
using Splat;

namespace AE.PID.Services;

public abstract class PageServiceBase : IDisposable, IEnableLogger
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