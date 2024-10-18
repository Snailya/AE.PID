using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace AE.PID.Visio.Shared.Extensions;

public static class ObservableExt
{
    public static IObservable<T> QuiescentLast<T>(
        this IObservable<T> src,
        TimeSpan minimumInactivityPeriod,
        IScheduler? scheduler = null)
    {
        scheduler ??= CurrentThreadScheduler.Instance;

        var onOffs =
            from _ in src
            from delta in
                Observable.Return(1, scheduler)
                    .Concat(Observable.Return(-1, scheduler)
                        .Delay(minimumInactivityPeriod, scheduler))
            select delta;
        var outstanding = onOffs.Scan(0, (total, delta) => total + delta);
        var zeroCrossings = outstanding.Where(total => total == 0);
        return src.Buffer(zeroCrossings).Select(x => x.Last());
    }

    public static IObservable<IList<T>> QuiescentBuffer<T>(
        this IObservable<T> src,
        TimeSpan minimumInactivityPeriod,
        IScheduler? scheduler = null)
    {
        scheduler ??= CurrentThreadScheduler.Instance;

        var onOffs =
            from _ in src
            from delta in
                Observable.Return(1, scheduler)
                    .Concat(Observable.Return(-1, scheduler)
                        .Delay(minimumInactivityPeriod, scheduler))
            select delta;
        var outstanding = onOffs.Scan(0, (total, delta) => total + delta);
        var zeroCrossings = outstanding.Where(total => total == 0);
        return src.Buffer(zeroCrossings);
    }
}