using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using AE.PID.Interfaces;
using DynamicData;
using DynamicData.Kernel;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class TreeNodeViewModelBase : ViewModelBase, IDisposable
{
    protected readonly CompositeDisposable CleanUp = new();
    private bool _isExpanded;
    private bool _isSelected;

    protected TreeNodeViewModelBase(int id, int depth, int parentId)
    {
        Id = id;
        Depth = depth;
        ParentId = parentId;
    }

    public int Id { get; }
    public int Depth { get; protected set; }
    public int ParentId { get; protected set; }

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

    public void Dispose()
    {
        CleanUp.Dispose();
    }
}

public class TreeNodeViewModel<TSource> : TreeNodeViewModelBase,
    IEquatable<TreeNodeViewModel<TSource>>
    where TSource : class, ITreeNode
{
    private readonly ReadOnlyObservableCollection<TreeNodeViewModel<TSource>> _inferiors;
    private TSource? _source;

    public TreeNodeViewModel(Node<TSource, int> node,
        TreeNodeViewModel<TSource>? parent = null) : base(node.Key, node.Depth, node.Item.ParentId)
    {
        Source = node.Item;
        Parent = parent;

        //Wrap loader for the nested view model inside a lazy so we can control when it is invoked
        var childrenLoader = node.Children.Connect()
            .Transform(e => (TreeNodeViewModel<TSource>)System.Activator.CreateInstance(
                typeof(TreeNodeViewModel<TSource>), e, this))
            .Bind(out _inferiors)
            .DisposeMany()
            .Subscribe()
            .DisposeWith(CleanUp);
    }

    public TSource? Source
    {
        get => _source;
        set => this.RaiseAndSetIfChanged(ref _source, value);
    }

    public Optional<TreeNodeViewModel<TSource>> Parent { get; }

    public ReadOnlyObservableCollection<TreeNodeViewModel<TSource>> Inferiors => _inferiors;


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