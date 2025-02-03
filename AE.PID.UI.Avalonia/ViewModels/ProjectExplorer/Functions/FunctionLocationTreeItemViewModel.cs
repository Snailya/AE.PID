using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using AE.PID.Client.Core;
using AE.PID.Core.Models;
using DynamicData;
using DynamicData.Binding;
using DynamicData.Kernel;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public class FunctionLocationTreeItemViewModel : ReactiveObject, IDisposable,
    IEquatable<FunctionLocationTreeItemViewModel>
{
    private readonly IDisposable _cleanUp;
    private readonly ReadOnlyObservableCollection<FunctionLocationTreeItemViewModel> _inferiors;
    private bool _isExpanded;
    private bool _isSelected;

    public FunctionLocationTreeItemViewModel(Node<FunctionLocation, ICompoundKey> node,
        FunctionLocationTreeItemViewModel? parent = null)
    {
        Id = node.Key;
        NodeName = node.Item.NodeName;

        Depth = node.Depth;
        Parent = parent;
        ParentId = node.Item.ParentId;

        Type = node.Item.Type;

        // Wrap loader for the nested view model inside a lazy so we can control when it is invoked
        var observeChildren = node.Children.Connect()
            .Transform(e => new FunctionLocationTreeItemViewModel(e, this))
            .SortAndBind(out _inferiors,
                SortExpressionComparer<FunctionLocationTreeItemViewModel>.Ascending(x => x.NodeName))
            .DisposeMany()
            .Subscribe();

        _cleanUp = Disposable.Create(() => { observeChildren.Dispose(); });
    }

    public int Depth { get; }
    public Optional<FunctionLocationTreeItemViewModel> Parent { get; }
    public ReadOnlyObservableCollection<FunctionLocationTreeItemViewModel> Inferiors => _inferiors;
    public ICompoundKey Id { get; }
    public ICompoundKey ParentId { get; set; }
    public FunctionType Type { get; set; }
    public string NodeName { get; set; }

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
        _cleanUp.Dispose();
    }

    #region Equality Members

    public bool Equals(FunctionLocationTreeItemViewModel other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(Id, other.Id);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((FunctionLocationTreeItemViewModel)obj);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }


    public static bool operator ==(FunctionLocationTreeItemViewModel left, FunctionLocationTreeItemViewModel right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(FunctionLocationTreeItemViewModel left, FunctionLocationTreeItemViewModel right)
    {
        return !Equals(left, right);
    }

    #endregion
}