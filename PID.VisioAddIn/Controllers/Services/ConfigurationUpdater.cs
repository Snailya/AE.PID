using System;
using System.Reactive;
using System.Reactive.Subjects;

namespace AE.PID.Controllers.Services;

public class ConfigurationUpdater
{
    /// <summary>
    /// Trigger used for ui Button to invoke the update event.
    /// </summary>
    public static Subject<Unit> ManuallyInvokeTrigger { get; } = new();

    /// <summary>
    ///     Emit a value manually
    /// </summary>
    public static void Invoke()
    {
        ManuallyInvokeTrigger.OnNext(Unit.Default);
    }
}