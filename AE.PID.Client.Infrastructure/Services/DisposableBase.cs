﻿using System;
using System.Reactive.Disposables;
using AE.PID.Client.Core;
using Splat;

namespace AE.PID.Client.Infrastructure;

public class DisposableBase : IDisposable, IEnableLogger
{
    protected readonly CompositeDisposable CleanUp = new();

    public virtual void Dispose()
    {
        if (this is IStore store)
        {
            store.Save();

            this.Log().Info("Store data saved.");
        }

        CleanUp.Dispose();
    }
}