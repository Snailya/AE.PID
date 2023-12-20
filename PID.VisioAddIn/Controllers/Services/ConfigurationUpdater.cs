using System;
using System.Reactive;
using System.Reactive.Subjects;

namespace AE.PID.Controllers.Services;

/// <summary>
/// This class handles the invoking of showing a configuration setup view from ribbon.
/// </summary>
public abstract class ConfigurationUpdater
{
    /// <summary>
    /// Trigger used for ui Button to invoke the update event.
    /// </summary>
    public static Subject<Unit> ManuallyInvokeTrigger { get; } = new();

    /// <summary>
    ///     Trigger manually.
    /// </summary>
    public static void Invoke()
    {
        ManuallyInvokeTrigger.OnNext(Unit.Default);
    }
}