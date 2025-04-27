using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Client.Core;
using AE.PID.Client.Core.VisioExt;
using AE.PID.Core;
using DynamicData;

namespace AE.PID.Client.VisioAddIn;

public class VirtualLocationGenerator : IDisposable
{
    private readonly Dictionary<Position, int> _positionToShapeIdDict = new();
    private readonly IDisposable _subscription;

    private readonly SourceCache<FunctionLocationEx, Position>
        _virtualLocations =
            new(x => x.Position);

    public VirtualLocationGenerator(IObservableCache<FunctionLocation, ICompoundKey> realLocations)
    {
        _subscription = realLocations.Connect()
            .Do(changes =>
            {
                using (_virtualLocations.SuspendCount())
                {
                    using (_virtualLocations.SuspendNotifications())
                    {
                        // 如果被删除的对象是代理功能组，或者其先辈功能组有关联的代理功能组，则需要移除之前生成的虚拟FunctionLocation
                        var toRemove = changes
                            .Where(x => x.Reason is ChangeReason.Remove)
                            .SelectMany(x =>
                            {
                                return x.Current switch
                                {
                                    { Type: FunctionType.ProcessZone } => [],
                                    { Type: FunctionType.FunctionGroup, IsProxy: true } =>
                                        _virtualLocations.Items.Where(i => Equals(i.Position.ProxyId, x.Current.Id)),
                                    { Type: FunctionType.FunctionGroup } => [],
                                    _ => _virtualLocations.Items.Where(i => Equals(i.Position.TargetId, x.Current.Id))
                                };
                            })
                            .Concat(changes.Where(x =>
                                    x.Reason is ChangeReason.Update
                                    && !Equals(x.Previous.Value.ParentId, x.Current.ParentId)
                                )
                                .SelectMany(x =>
                                {
                                    return x.Current switch
                                    {
                                        { Type: FunctionType.ProcessZone } => [],
                                        { Type: FunctionType.FunctionGroup, IsProxy: true } =>
                                            _virtualLocations.Items.Where(i =>
                                                Equals(i.Position.ProxyId, x.Previous.Value.Id)),
                                        { Type: FunctionType.FunctionGroup } => [],
                                        _ => _virtualLocations.Items.Where(i =>
                                            Equals(i.Position.TargetId, x.Previous.Value.Id))
                                    };
                                }))
                            .ToArray();

                        // 当更新对象时，有多种情况：
                        // 1. 更新对象是不是工艺区域或代理功能组时，需要检查它或者它的先辈是否有关联的代理功能组，如果是，则需要更新所有的代理功能组生成的虚拟FunctionLocation
                        // 2. 更新对象是代理功能组时，需要重新生成虚拟的FunctionLocation
                        // 这样子保证了生成子树的完全正确性，但是考虑到可能有一部分节点并不需要更新，因此需要和cache中的项进行比较，仅对变化的项进行通知。
                        var toUpdate =
                            (from change in changes
                                where change.Reason is ChangeReason.Add or ChangeReason.Update
                                from proxy in change.Current switch
                                {
                                    { Type: FunctionType.ProcessZone } => [],
                                    { Type: FunctionType.FunctionGroup, IsProxy: true } => [change.Current],
                                    { Type: FunctionType.FunctionGroup } => FindRelatedProxyFunctionGroups(
                                        change.Current, realLocations),
                                    _ => FindRelatedProxyFunctionGroups(change.Current, realLocations)
                                }
                                join source in realLocations.Items on proxy.TargetId equals source.Id
                                select new
                                {
                                    FunctionGroup = source,
                                    ProxyFunctionGroup = proxy,
                                    Position = new Position { ProxyId = proxy.Id, TargetId = source.TargetId }
                                })
                            .SelectMany(x => FindDescendents(realLocations.Items, x.FunctionGroup)
                                .Select(i => new FunctionLocationEx
                                {
                                    Source = i,
                                    FunctionGroup = x.FunctionGroup,
                                    ProxyFunctionGroup = x.ProxyFunctionGroup,
                                    Position = new Position { ProxyId = x.ProxyFunctionGroup.Id, TargetId = i.Id }
                                }))
                            .Distinct()
                            .ToArray();

                        // 当删除的对象不需要被重新生成时，删除对象
                        foreach (var item in toRemove)
                            if (toUpdate.SingleOrDefault(x => Equals(x.Position, item.Position)) is
                                null)
                                _virtualLocations.Remove(item);

                        // 更新对象
                        // Hack: this reverse is very critical, to make the TransformToTree works as expected, the function element must come after parent equipment in changeset, otherwise the tree node for the virtual function element will not update.
                        foreach (var item in toUpdate.Reverse())
                        {
                            // 如果这个item压根不存在
                            var previous = _virtualLocations.Lookup(
                                new Position
                                {
                                    ProxyId = item.Position.ProxyId,
                                    TargetId = item.Position.TargetId
                                });

                            if (previous.HasValue)
                            {
                                // 比较source是否发生变化, 如果proxy function group 发生变化，会影响子节点的属性
                                if (previous.Value.Source != item.Source ||
                                    changes.Any(x => Equals(x.Current.Id, item.ProxyFunctionGroup.Id)))
                                    _virtualLocations.AddOrUpdate(item);
                            }
                            else
                            {
                                _virtualLocations.AddOrUpdate(item);
                            }
                        }
                    }
                }
            })
            .Subscribe();

        VirtualLocations = _virtualLocations.Connect()
            .Transform(ToFunctionLocation)
            .ChangeKey(x => x.Id)
            .AsObservableCache();
    }


    /// <summary>
    ///     Provide virtual function locations based on real locations
    /// </summary>
    public IObservableCache<FunctionLocation, ICompoundKey> VirtualLocations { get; }

    public void Dispose()
    {
        _subscription.Dispose();
    }

    /// <summary>
    ///     Build virtual function location from source.
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    private FunctionLocation ToFunctionLocation(FunctionLocationEx location)
    {
        VisioShapeId? parentId;

        // if the source is the children of the function group, the virtual one ought to directly attach to the proxy function group, so use the proxy function group's id as the parent id
        if (Equals(location.Source.ParentId, location.FunctionGroup.Id))
        {
            parentId = location.ProxyFunctionGroup.Id as VisioShapeId;
        }
        // otherwise, the id of the virtual parent should be used
        else
        {
            var parentKey = location.Position with { TargetId = location.Source.ParentId! };
            if (!_positionToShapeIdDict.ContainsKey(parentKey))
                _positionToShapeIdDict.Add(parentKey,
                    _positionToShapeIdDict.Count == 0 ? 1 : _positionToShapeIdDict.Max(i => i.Value) + 1);
            var parentShapeId = _positionToShapeIdDict[parentKey];
            parentId = new VisioShapeId(-1, parentShapeId);
        }

        var key = location.Position with { TargetId = location.Source.Id };
        if (!_positionToShapeIdDict.ContainsKey(key))
            _positionToShapeIdDict.Add(key,
                _positionToShapeIdDict.Count == 0 ? 1 : _positionToShapeIdDict.Max(i => i.Value) + 1);
        var shapeId = _positionToShapeIdDict[key];

        return location.Source with
        {
            Id = new VisioShapeId(-1, shapeId),
            ParentId = parentId,
            IsVirtual = true,
            IsProxy = false,
            TargetId = location.Source.Id,
            Zone = location.ProxyFunctionGroup.Zone,
            ZoneEnglishName = location.ProxyFunctionGroup.ZoneEnglishName,
            ZoneName = location.ProxyFunctionGroup.ZoneName,
            Group = location.ProxyFunctionGroup.Group,
            GroupEnglishName = location.ProxyFunctionGroup.GroupEnglishName,
            GroupName = location.ProxyFunctionGroup.GroupName,
            ProxyGroupId = location.ProxyFunctionGroup.Id
        };
    }

    /// <summary>
    ///     Find out the proxy function groups that target to the item's parent function group.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="realLocations"></param>
    /// <returns></returns>
    private static IEnumerable<FunctionLocation> FindRelatedProxyFunctionGroups(FunctionLocation item,
        IObservableCache<FunctionLocation, ICompoundKey> realLocations)
    {
        if (item is { IsProxy: true, Type: FunctionType.FunctionGroup }) return [item];

        var current = item;

        while (current is { ParentId: not null })
        {
            var parent = realLocations.Items.FirstOrDefault(n => Equals(n.Id, current.ParentId));
            if (parent?.Type == FunctionType.FunctionGroup)
                return realLocations.Items.Where(x => Equals(x.TargetId, parent.Id)).ToArray();

            current = parent;
        }

        return [];
    }

    /// <summary>
    ///     Find the descendants that belong to the location.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    private static IEnumerable<FunctionLocation> FindDescendents(IReadOnlyList<FunctionLocation> source,
        FunctionLocation parent)
    {
        var children = source.Where(x => Equals(x.ParentId, parent.Id)).ToArray();
        return children.SelectMany(x => FindDescendents(source, x)).Concat(children);
    }

    private class FunctionLocationEx
    {
        /// <summary>
        ///     The actual function location used as the source for generate the virtual item.
        /// </summary>
        public FunctionLocation Source { get; set; }

        /// <summary>
        ///     The parent function group of the source.
        /// </summary>
        public FunctionLocation FunctionGroup { get; set; }

        /// <summary>
        ///     The proxy function group that the generated virtual item should belong to.
        /// </summary>
        public FunctionLocation ProxyFunctionGroup { get; set; }

        /// <summary>
        ///     The position key used to locate the virtual item.
        /// </summary>
        public Position Position { get; set; }

        public override string ToString()
        {
            return $"{{Position:{Position}, Source:{Source}}}";
        }
    }

    private record struct Position
    {
        /// <summary>
        ///     The source item's id.
        /// </summary>
        public ICompoundKey TargetId { get; set; }

        /// <summary>
        ///     The id of the proxy function group which the generated virtual item belongs to.
        /// </summary>
        public ICompoundKey ProxyId { get; set; }

        public override string ToString()
        {
            return $"{{ProxyId: {ProxyId}, TargetId: {TargetId} }}";
        }
    }
}