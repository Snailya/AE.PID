using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DynamicData;

namespace AE.PID.Client.Infrastructure;

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

    public static IObservable<IChangeSet<TObject, TKey>> DebugLog<TObject, TKey>(
        this IObservable<IChangeSet<TObject, TKey>> src, [CallerMemberName] string callerName = "")
        where TKey : notnull where TObject : notnull
    {
        var boundary = DateTime.Now.Ticks;
        // 如果 callerName 是构造函数（.ctor），尝试获取类名
        if (callerName == ".ctor" || callerName == ".cctor")
        {
            // 通过调用栈获取类名（性能稍差，但更准确）
            var stackTrace = new StackTrace();
            var frame = stackTrace.GetFrame(1); // 获取调用者的栈帧
            var method = frame.GetMethod();
            callerName = method.DeclaringType?.Name ?? "UnknownClass";
        }

        return src.Do(changes =>
        {
            Debug.WriteLine($"------{callerName}--{boundary}");

            foreach (var change in changes) Debug.WriteLine(change);

            Debug.WriteLine($"----BOUNDARY{boundary}");
        });
    }
}