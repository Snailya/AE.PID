using System.Reactive.Concurrency;

namespace AE.PID.Visio.Shared;

public static class SchedulerManager
{
    public static IScheduler VisioScheduler = null!;
}