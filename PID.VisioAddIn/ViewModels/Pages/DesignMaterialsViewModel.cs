using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Controllers.Services;
using AE.PID.Core.DTOs;
using AE.PID.Models.BOM;
using AE.PID.Models.EventArgs;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.ViewModels.Pages;

public class DesignMaterialsViewModel(MaterialsService service) : ViewModelBase
{
    private ReadOnlyObservableCollection<DesignMaterialCategoryViewModel> _categories = new([]);
    private string _elementName = string.Empty;
    private ObservableAsPropertyHelper<ReadOnlyCollection<DesignMaterialCategoryViewModel>> _filterdCategories;

    private ReadOnlyObservableCollection<DesignMaterial> _lastUsed = new([]);

    private int _pageNumber = 1;
    private DesignMaterialCategoryViewModel? _selectedCategory;

    private ReadOnlyObservableCollection<DesignMaterial>? _validMaterials;


    protected override void SetupCommands()
    {
        // when an item is selected, it should be add to the last used grid for future use
        Select = ReactiveCommand.Create<DesignMaterial, DesignMaterial>(material =>
        {
            AddToLastUsed(material, _elementName);
            return material;
        });

        // create a hack command so that other class could observe this action
        // this is used to trigger load more action of the lazy load data grid in view class
        Load = ReactiveCommand.Create(() => { });
    }

    protected override void SetupSubscriptions(CompositeDisposable d)
    {
        // listen for element selected event in Export page, when the selected element changed, it is used as the seed for this page
        MessageBus.Current.Listen<ElementSelectedEventArgs>()
            .DistinctUntilChanged()
            .Subscribe(x => ElementName = x.Name)
            .DisposeWith(d);

        // when an item is selected by user, notify Export page for selection.
        MessageBus.Current.RegisterMessageSource(Select!.Select(x => new DesignMaterialSelectedEventArgs(x)))
            .DisposeWith(d);

        // when the category if fetched from server, it is originally a flatten list
        // convert it into a tree structure using dynamic data
        // however, to enhance user select efficiency, this tree is not used directly but as the source for a filtered tree that matches the current element
        service.Categories
            .Connect()
            .TransformToTree(x => x.ParentId, Observable.Return(DefaultPredicate))
            .Transform(node => new DesignMaterialCategoryViewModel(node))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _categories)
            .DisposeMany()
            .Subscribe()
            .DisposeWith(d);

        // filter category on element name change, each element name is matched to one or more node in the category
        this.WhenAnyValue(x => x.ElementName)
            .Merge(_categories.ObserveCollectionChanges().Select(_ => ElementName))
            .Select(x =>
            {
                if (service.CategoryMap.TryGetValue(x, out var ids))
                    return new ReadOnlyCollection<DesignMaterialCategoryViewModel>(
                        _categories
                            .SelectMany(i => FilterNodeByCode(i, ids)).Where(i => i != null)
                            .ToList()
                    );

                return _categories;
            })
            .ToProperty(this, x => x.FilteredCategories, out _filterdCategories)
            .DisposeWith(d);

        // whenever the category changed, reset to page number to 1 to make sure the first page is loaded
        this.WhenAnyValue(x => x.SelectedCategory)
            .Subscribe(_ => { PageNumber = 1; })
            .DisposeWith(d);

        // to enhance the performance, only the first page is load when the category first selected.
        // load more data by increasing page number. this leads to emit a new query, which leads to populate materials later.
        Load?.Throttle(TimeSpan.FromMilliseconds(300))
            .Subscribe(_ => PageNumber += 1)
            .DisposeWith(d);

        // a query is build to make it reusable for both data filters, and for fetch more data
        var query = this.WhenAnyValue(x => x.SelectedCategory,
                x => x.PageNumber,
                (selectedCategory, pageNumber) => selectedCategory == null
                    ? null
                    : new DesignMaterialsQueryTerms(selectedCategory.Id, pageNumber)
            )
            .WhereNotNull();

        // the load action is firstly process by request server to fetch materials if not in cache
        query.Subscribe(x => { _ = service.PopulateMaterials(x); })
            .DisposeWith(d);

        // then valid materials is filter from service
        var queryFilter = query
            .WhereNotNull()
            .Select(BuildCategoryFilter);
        // also, users might create some custom filter from the view
        var userFilter = this.WhenAnyValue(
                t => t.UserFiltersViewModel.Name,
                t => t.UserFiltersViewModel.Brand,
                t => t.UserFiltersViewModel.Specifications,
                t => t.UserFiltersViewModel.Model,
                t => t.UserFiltersViewModel.Manufacturer
            )
            .Select(BuildUserFilter);
        service.Materials
            .Connect()
            .Filter(queryFilter)
            .RemoveKey()
            .TransformMany(x => x.Materials)
            .WhereNotNull()
            .Filter(userFilter)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _validMaterials)
            .DisposeMany()
            .Subscribe()
            .DisposeWith(d);

        // create a filter for LastUsed list to filter only current category
        var lastUsedFilter = this.WhenValueChanged(t => t.SelectedCategory)
            .WhereNotNull()
            .Select(x => x.Id)
            .Select(BuildLastUsedFilter);
        service.LastUsed
            .Connect()
            .Filter(lastUsedFilter)
            .SortBy(x => x.LastUsed)
            .Transform(x => x.Source)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _lastUsed)
            .DisposeMany()
            .Subscribe()
            .DisposeWith(d);

        return;

        bool DefaultPredicate(Node<MaterialCategoryDto, int> node)
        {
            return node.IsRoot;
        }
    }


    /// <summary>
    ///     Add design material to last used list.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="selectedName"></param>
    private DesignMaterial AddToLastUsed(DesignMaterial item, string selectedName)
    {
        service.AddToLastUsed(item, selectedName);
        return item;
    }

    /// <summary>
    ///     Filter the subtree by code
    /// </summary>
    /// <param name="node"></param>
    /// <param name="codes"></param>
    /// <returns></returns>
    private static IEnumerable<DesignMaterialCategoryViewModel> FilterNodeByCode(DesignMaterialCategoryViewModel node,
        string[] codes)
    {
        var result = new List<DesignMaterialCategoryViewModel>();
        if (codes.Contains(node.Code)) return [node];

        foreach (var child in node.Inferiors)
            if (FilterNodeByCode(child, codes) is { } founded)
                result.AddRange(founded);

        return result;
    }

    /// <summary>
    ///     Filter the last used items by category
    /// </summary>
    /// <param name="categoryId"></param>
    /// <returns></returns>
    private static Func<LastUsedDesignMaterial, bool> BuildLastUsedFilter(int categoryId)
    {
        return m => m.Source.Categories.Contains(categoryId);
    }

    /// <summary>
    ///     Filter the design materials by cateogry
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    private static Func<MaterialsRequestResult, bool> BuildCategoryFilter(DesignMaterialsQueryTerms query)
    {
        return m => m.QueryTerms.CategoryId == query.CategoryId &&
                    m.QueryTerms.PageNumber <= query.PageNumber;
    }

    /// <summary>
    ///     Filter the design materials by user conditions
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    private static Func<DesignMaterial, bool> BuildUserFilter((string, string, string, string, string) arg)
    {
        var (name, brand, specifications, model, manufacturer) = arg;

        return m => m.Name.Contains(name) && m.Brand.Contains(brand) && m.Specifications.Contains(specifications) &&
                    m.Model.Contains(model) && m.Manufacturer.Contains(manufacturer);
    }

    #region Read-Write Properties

    /// <summary>
    ///     The name is the seed for the this view model. The name is mapped to a category and then all girds on populated by
    ///     this category.
    /// </summary>
    public string ElementName
    {
        get => _elementName;
        set => this.RaiseAndSetIfChanged(ref _elementName, value);
    }

    /// <summary>
    ///     SelectedCategory determines materials from which category should be load.
    /// </summary>
    public DesignMaterialCategoryViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
    }

    /// <summary>
    ///     The current page indicator, which controls materials load and request.
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = this.RaiseAndSetIfChanged(ref _pageNumber, value);
    }

    #endregion

    #region Read-Only Properties

    public ReactiveCommand<Unit, Unit>? Load { get; private set; }
    public ReactiveCommand<DesignMaterial, DesignMaterial>? Select { get; set; }
    public ReactiveCommand<Unit, Unit>? Close { get; set; }

    #endregion

    #region Output Proeprties

    public ReadOnlyObservableCollection<DesignMaterialCategoryViewModel> Categories => _categories;
    public ReadOnlyObservableCollection<DesignMaterial> LastUsed => _lastUsed;
    public ReadOnlyObservableCollection<DesignMaterial>? ValidMaterials => _validMaterials;
    public UserFiltersViewModel UserFiltersViewModel { get; set; } = new();
    public ReadOnlyCollection<DesignMaterialCategoryViewModel> FilteredCategories => _filterdCategories.Value;

    #endregion
}