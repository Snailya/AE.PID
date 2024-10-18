using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Core.Interfaces;
using AE.PID.Visio.Core;
using DynamicData;
using DynamicData.Kernel;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class TreeNodeViewModelBase : ViewModelBase
{
    private bool _isExpanded;
    private bool _isSelected;
    private string _name = string.Empty;

    protected TreeNodeViewModelBase(int id, int depth, int parentId, string name)
    {
        Id = id;
        Depth = depth;
        ParentId = parentId;
        Name = name;
    }

    public int Id { get; }
    public int Depth { get; protected set; }
    public int ParentId { get; protected set; }

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }
}

public class TreeNodeViewModel<TSource> : TreeNodeViewModelBase, IDisposable,
    IEquatable<TreeNodeViewModel<TSource>>
    where TSource : class, ITreeNode
{
    private readonly CompositeDisposable _cleanUp = new();

    private readonly ReadOnlyObservableCollection<TreeNodeViewModel<TSource>> _inferiors;

    public TreeNodeViewModel(Node<TSource, int> node,
        TreeNodeViewModel<TSource>? parent = null) : base(node.Key, node.Depth, node.Item.ParentId, node.Item.Name)
    {
        Parent = parent;

        node.Children.Connect()
            .Transform(e => Create(e, this))
            .ObserveOn(App.UIScheduler)
            .Bind(out _inferiors)
            .DisposeMany()
            .Subscribe()
            .DisposeWith(_cleanUp);
    }

    public Optional<TreeNodeViewModel<TSource>> Parent { get; }

    public ReadOnlyObservableCollection<TreeNodeViewModel<TSource>> Inferiors => _inferiors;

    public void Dispose()
    {
        _cleanUp.Dispose();
    }

    public TreeNodeViewModel<TSource> Create(Node<TSource, int> node, TreeNodeViewModel<TSource>? parent = null)
    {
        return new TreeNodeViewModel<TSource>(node, parent);
    }


    #region Equality Members

    public bool Equals(TreeNodeViewModel<TSource> other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((TreeNodeViewModel<TSource>)obj);
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public static bool operator ==(TreeNodeViewModel<TSource> left,
        TreeNodeViewModel<TSource> right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TreeNodeViewModel<TSource> left,
        TreeNodeViewModel<TSource> right)
    {
        return !Equals(left, right);
    }

    #endregion
}