using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Visio.Core.Models;
using Avalonia.Data;
using DynamicData;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class MaterialCategoryViewModel : ReactiveObject, IDisposable, IEquatable<MaterialCategoryViewModel>
{
    private readonly IDisposable _cleanUp;
    private readonly ReadOnlyObservableCollection<MaterialCategoryViewModel> _inferiors;
    private string _countText;
    private bool _isSelected;
    private bool _isValid;

    public MaterialCategoryViewModel(Node<MaterialCategory, int> node, MaterialCategoryViewModel parent = null)
    {
        Id = node.Key;
        Name = node.Item.Name;
        Depth = node.Depth;
        Parent = parent;
        ParentId = node.Item.ParentId;
        Source = node.Item;


        //Wrap loader for the nested view model inside a lazy so we can control when it is invoked
        var observeChildren = node.Children.Connect()
            .Transform(e => new MaterialCategoryViewModel(e, this))
            .Bind(out _inferiors)
            .DisposeMany()
            .Subscribe();

        //create some display text based on the number of employees
        var count = node.Children.CountChanged
            .Select(count => $"({count})")
            .Subscribe(text => CountText = text);

        _cleanUp = Disposable.Create(() =>
        {
            count.Dispose();
            observeChildren.Dispose();
        });
    }

    public string CountText
    {
        get => _countText;
        set => this.RaiseAndSetIfChanged(ref _countText, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    public int Depth { get; }
    public Optional<MaterialCategoryViewModel> Parent { get; }
    public ReadOnlyObservableCollection<MaterialCategoryViewModel> Inferiors => _inferiors;
    public MaterialCategory Source { get; }

    public int Id { get; set; }
    public int ParentId { get; set; }
    public string Name { get; set; }

    public bool IsValid
    {
        get => _isValid;
        set => this.RaiseAndSetIfChanged(ref _isValid, value);
    }

    public void Dispose()
    {
        _cleanUp.Dispose();
    }

    #region Equality Members

    public bool Equals(MaterialCategoryViewModel other)
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
        return Equals((MaterialCategoryViewModel)obj);
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public static bool operator ==(MaterialCategoryViewModel? left, MaterialCategoryViewModel? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(MaterialCategoryViewModel? left, MaterialCategoryViewModel? right)
    {
        return !Equals(left, right);
    }

    #endregion
}