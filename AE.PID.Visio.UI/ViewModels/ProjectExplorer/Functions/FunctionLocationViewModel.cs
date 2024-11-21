using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using AE.PID.Core.Models;
using AE.PID.Visio.Core.Models;
using DynamicData;
using DynamicData.Binding;
using DynamicData.Kernel;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class FunctionLocationViewModel : ReactiveObject, IDisposable, IEquatable<FunctionLocationViewModel>
{
    private readonly IDisposable _cleanUp;
    private readonly ReadOnlyObservableCollection<FunctionLocationViewModel> _inferiors;
    private bool _isExpanded;
    private bool _isSelected;
    private string _name = string.Empty;

    public FunctionLocationViewModel(Node<FunctionLocation, CompositeId> node, FunctionLocationViewModel? parent = null)
    {
        Id = node.Key;
        Name = node.Item.NodeName;

        Depth = node.Depth;
        Parent = parent;
        ParentId = node.Item.ParentId;
        Source = node.Item;

        // Wrap loader for the nested view model inside a lazy so we can control when it is invoked
        var observeChildren = node.Children.Connect()
            .Transform(e => new FunctionLocationViewModel(e, this))
            .SortAndBind(out _inferiors, SortExpressionComparer<FunctionLocationViewModel>.Ascending(x => x.Name))
            .DisposeMany()
            .Subscribe();

        _cleanUp = Disposable.Create(() => { observeChildren.Dispose(); });
    }

    public int Depth { get; }
    public Optional<FunctionLocationViewModel> Parent { get; }
    public ReadOnlyObservableCollection<FunctionLocationViewModel> Inferiors => _inferiors;
    public FunctionLocation Source { get; }
    public CompositeId Id { get; }
    public CompositeId ParentId { get; set; }

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

    public void Dispose()
    {
        _cleanUp.Dispose();
    }

    #region Equality Members

    public bool Equals(FunctionLocationViewModel other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((FunctionLocationViewModel)obj);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(FunctionLocationViewModel left, FunctionLocationViewModel right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(FunctionLocationViewModel left, FunctionLocationViewModel right)
    {
        return !Equals(left, right);
    }

    #endregion
}