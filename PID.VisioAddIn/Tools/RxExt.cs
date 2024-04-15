using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace AE.PID.Tools;

internal static class RxExt
{
    public static IObservable<IList<T>> Quiescent<T>(
        this IObservable<T> src,
        TimeSpan minimumInactivityPeriod,
        IScheduler scheduler)
    {
        var onoffs =
            from _ in src
            from delta in
                Observable.Return(1, scheduler)
                    .Concat(Observable.Return(-1, scheduler)
                        .Delay(minimumInactivityPeriod, scheduler))
            select delta;
        var outstanding = onoffs.Scan(0, (total, delta) => total + delta);
        var zeroCrossings = outstanding.Where(total => total == 0);
        return src.Buffer(zeroCrossings);
    }
}