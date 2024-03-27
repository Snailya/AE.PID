using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using AE.PID.Core.DTOs;
using DynamicData;
using DynamicData.Kernel;

namespace AE.PID.ViewModels;

public class DesignMaterialCategoryViewModel : ViewModelBase, IDisposable
{
    private readonly IDisposable _cleanUp;
    private readonly ReadOnlyObservableCollection<DesignMaterialCategoryViewModel> _inferiors;

    public DesignMaterialCategoryViewModel(Node<MaterialCategoryDto, int> node,
        DesignMaterialCategoryViewModel? parent = null)
    {
        Id = node.Key;
        Depth = node.Depth;
        Parent = parent;
        ParentId = node.Item.ParentId;
        Source = node.Item;

        Name = node.Item.Name;
        Code = node.Item.Code;

        // todo: maybe should use lazy loading
        var childrenLoader = node.Children.Connect()
            .Transform(e => new DesignMaterialCategoryViewModel(e, this))
            .Bind(out _inferiors)
            .DisposeMany()
            .Subscribe();

        _cleanUp = Disposable.Create(() => { childrenLoader.Dispose(); });
    }

    /// <summary>
    ///     The id of the item
    /// </summary>
    public int Id { get; }

    /// <summary>
    ///     The level of the item
    /// </summary>
    public int Depth { get; }

    /// <summary>
    ///     The parent id of the item, used to reconstruct for tree
    /// </summary>
    public int? ParentId { get; }

    /// <summary>
    ///     The origin data
    /// </summary>
    public MaterialCategoryDto Source { get; }

    /// <summary>
    ///     The parent of the item.
    /// </summary>
    public Optional<DesignMaterialCategoryViewModel> Parent { get; }

    /// <summary>
    ///     The children of the item
    /// </summary>
    public ReadOnlyObservableCollection<DesignMaterialCategoryViewModel> Inferiors => _inferiors;

    /// <summary>
    ///     The display name of the category.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     The user toward identifier
    /// </summary>
    public string Code { get; set; }

    public void Dispose()
    {
        _cleanUp.Dispose();
    }
}