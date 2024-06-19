using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Dtos;
using AE.PID.EventArgs;
using AE.PID.Models;
using AE.PID.Services;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class DesignMaterialsViewModel(MaterialsService service) : ViewModelBase
{
    // Members that return a sequence should never return null
    private ReadOnlyObservableCollection<TreeNodeViewModel<MaterialCategoryDto>> _categories = new([]);

    private ObservableAsPropertyHelper<string>? _categoryFilterSeed;

    private ElementBase? _element;
    private ReadOnlyObservableCollection<DesignMaterial> _lastUsed = new([]);
    private int _pageNumber = 1;
    private TreeNodeViewModel<MaterialCategoryDto>? _selectedCategory;
    private ReadOnlyObservableCollection<DesignMaterial> _validMaterials = new([]);


    #region Command Handlers

    private void WriteMaterialAndAddToLastUsed(DesignMaterial material)
    {
        if (_element is not PartItem partItem) return;

        partItem.MaterialNo = material.Code;
        service.AddToLastUsed(material, CategoryPredicateSeed);
    }

    #endregion

    #region Setups

    protected override void SetupCommands()
    {
        // when an item is selected, it should be added to the last used grid for future use
        Select = ReactiveCommand.Create<DesignMaterial>(WriteMaterialAndAddToLastUsed);

        // create a hack command so that other class could observe this action
        // this is used to trigger load more action of the lazy load data grid in view class
        Load = ReactiveCommand.Create(() => { });

        Close = ReactiveCommand.Create(() => { });
    }

    protected override void SetupSubscriptions(CompositeDisposable d)
    {
        // listen for element selected event in Export page, when the selected element changed, it is used as the seed for this page
        // todo: not invoke if select from page on the same element
        MessageBus.Current.Listen<ElementSelectedEventArgs>()
            .DistinctUntilChanged()
            .Select(x => x.ElementBase)
            .Subscribe(x => Element = x)
            .DisposeWith(d);

        // build up category predicate seed based on an element
        this.WhenAnyValue(x => x.Element)
            .Select(x =>
            {
                return x switch
                {
                    FunctionalGroup or EquipmentUnit => string.Empty,
                    Equipment equipment => equipment.SubClassName,
                    FunctionalElement functionalElement => functionalElement.Designation,
                    _ => string.Empty
                };
            })
            .ToProperty(this, x => x.CategoryPredicateSeed, out _categoryFilterSeed)
            .DisposeWith(d);

        // when the category if fetched from server, it is originally a flattened list
        // convert it into a tree structure using dynamic data,
        // however, to enhance user select efficiency, this tree is not used directly but as the source for a filtered tree that matches the current element
        var categoryPredicate = this.WhenAnyValue(x => x.CategoryPredicateSeed)
            .Select(BuildCategoryPredicate());
        service.Categories
            .Connect()
            .TransformToTree(x => x.ParentId, categoryPredicate)
            .Transform(node => new TreeNodeViewModel<MaterialCategoryDto>(node))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _categories)
            .DisposeMany()
            .Subscribe()
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

        // then valid materials are filter from service
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
        service.MaterialsGroupByCategory
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
    }

    protected override void SetupDeactivate()
    {
        Element?.Dispose();
    }

    private Func<string, Func<Node<MaterialCategoryDto, int>, bool>> BuildCategoryPredicate()
    {
        return name => string.IsNullOrEmpty(name)
            ? _ => false
            : node =>
                service.CategoryMap.TryGetValue(name, out var codes) ? codes.Contains(node.Item.Code) : node.IsRoot;
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
    ///     Filter the design materials by category
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    private static Func<MaterialsRequestResult, bool> BuildCategoryFilter(DesignMaterialsQueryTerms query)
    {
        return m => m.QueryTerms?.CategoryId == query.CategoryId &&
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

    #endregion

    #region Read-Write Properties

    /// <summary>
    ///     The name is the seed for this view model.
    ///     The name is mapped to a category and then all girds on populated by this category.
    /// </summary>
    public ElementBase? Element
    {
        get => _element;
        set => this.RaiseAndSetIfChanged(ref _element, value);
    }

    /// <summary>
    ///     SelectedCategory determines materials from which category should be load.
    /// </summary>
    public TreeNodeViewModel<MaterialCategoryDto>? SelectedCategory
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
    public ReactiveCommand<DesignMaterial, Unit>? Select { get; set; }
    public ReactiveCommand<Unit, Unit>? Close { get; set; }

    #endregion

    #region Output Proeprties

    public string CategoryPredicateSeed => _categoryFilterSeed.Value;
    public ReadOnlyObservableCollection<TreeNodeViewModel<MaterialCategoryDto>> Categories => _categories;
    public ReadOnlyObservableCollection<DesignMaterial> LastUsed => _lastUsed;
    public ReadOnlyObservableCollection<DesignMaterial> ValidMaterials => _validMaterials;
    public UserFiltersViewModel UserFiltersViewModel { get; set; } = new();

    #endregion
}