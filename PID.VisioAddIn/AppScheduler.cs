using System.Reactive.Concurrency;

namespace AE.PID;

public static class AppScheduler
{
    /// <summary>
    ///     The scheduler used for schedule unit of work on Visio thread.
    /// </summary>
    public static IScheduler VisioScheduler;

    /// <summary>
    ///     The scheduler used for schedule unit of work on UI thread
    /// </summary>
    public static IScheduler UIScheduler;
}