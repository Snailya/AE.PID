using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using FluentAssertions;

namespace AE.PID.Test;

public class UnitTest1
{
    [Fact]
    public void PopulateIntoShouldNotRemoveSourceAfterDispose()
    {
        var source = new SourceCache<KeyValue, int>(x => x.Key);
        var target = new SourceCache<KeyValue, int>(x => x.Key);

        var subscription = source.Connect().PopulateInto(target);
        source.AddOrUpdate(new KeyValue { Key = 1, Value = 2 });
        target.Items.Count.Should().Be(source.Items.Count);

        // if one source is populated into another source, after dispose of the original source, the data will not disappear in the target source.
        subscription.Dispose();
        target.Items.Count.Should().Be(source.Items.Count);

        source.AddOrUpdate(new KeyValue { Key = 2, Value = 2 });
        target.Items.Count.Should().NotBe(source.Items.Count);
    }

    [Fact]
    public void DisposeParentShouldAlsoInvokeDisposeChild()
    {
        var isSubscription1Disposed = false;
        var isSubscription2Disposed = false;

        var innerSubscription1 = Observable.Create<Unit>(subscriber =>
        {
            subscriber.OnNext(Unit.Default);
            return Disposable.Create(() => isSubscription1Disposed = true);
        }).Subscribe();
        var innerSubscription2 = Observable.Create<Unit>(subscriber =>
        {
            subscriber.OnNext(Unit.Default);
            return Disposable.Create(() => isSubscription2Disposed = true);
        }).Subscribe();

        isSubscription1Disposed.Should().BeFalse();
        isSubscription2Disposed.Should().BeFalse();

        var disposeSubscription1Trigger = new Subject<Unit>();
        var triggerSubscription = disposeSubscription1Trigger.Subscribe(_ => innerSubscription1.Dispose());

        var subscription = new CompositeDisposable(innerSubscription1, innerSubscription2, triggerSubscription);

        disposeSubscription1Trigger.OnNext(Unit.Default);
        subscription.Dispose();
        isSubscription1Disposed.Should().BeTrue();
        isSubscription2Disposed.Should().BeTrue();
    }

    public class KeyValue
    {
        public int Key { get; set; }
        public int Value { get; set; }
    }
}