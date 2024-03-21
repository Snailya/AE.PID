using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Controllers.Services;
using AE.PID.Core.DTOs;
using AE.PID.Models.BOM;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.ViewModels.Pages;

public class DesignMaterialsViewModel(MaterialsService service) : ViewModelBase
{
    private string _elementName = string.Empty;

    private ReadOnlyObservableCollection<DesignMaterialCategoryViewModel> _categories = new([]);
    private DesignMaterialCategoryViewModel? _selectedCategory;

    private int _pageNumber = 1;

    private ReadOnlyObservableCollection<DesignMaterial> _lastUsed = new([]);

    private ReadOnlyObservableCollection<DesignMaterial>? _validMaterials;

    #region Read-Write Properties

    /// <summary>
    /// The name is the seed for the this view model. The name is mapped to a category and then all girds on populated by this category.
    /// </summary>
    public string ElementName
    {
        get => _elementName;
        set => this.RaiseAndSetIfChanged(ref _elementName, value);
    }

    /// <summary>
    /// SelectedCategory determines materials from which category should be load.
    /// </summary>
    public DesignMaterialCategoryViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
    }

    /// <summary>
    /// The current page indicator, which controls materials load and request.
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

    public ReadOnlyObservableCollection<DesignMaterialCategoryViewModel> Categories => _categories;
    public ReadOnlyObservableCollection<DesignMaterial> LastUsed => _lastUsed;
    public ReadOnlyObservableCollection<DesignMaterial>? ValidMaterials => _validMaterials;
    public UserFiltersViewModel UserFiltersViewModel { get; set; } = new();

    #endregion

    protected override void SetupCommands()
    {
        Select = ReactiveCommand.Create<DesignMaterial>(material => AddToLastUsed(material, _elementName));
        Load = ReactiveCommand.Create(() => { });
    }

    protected override void SetupSubscriptions(CompositeDisposable d)
    {
        // covert category items from service to a tree structure and output as categories
        service.Categories
            .Connect()
            .TransformToTree(x => x.ParentId, Observable.Return(DefaultPredicate))
            .Transform(node => new DesignMaterialCategoryViewModel(node))
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
    /// Add design material to last used list.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="selectedName"></param>
    public void AddToLastUsed(DesignMaterial item, string selectedName)
    {
        service.AddToLastUsed(item, selectedName);
    }

    private static Func<LastUsedDesignMaterial, bool> BuildLastUsedFilter(int categoryId)
    {
        return m => m.Source.Categories.Contains(categoryId);
    }

    private static Func<MaterialsRequestResult, bool> BuildCategoryFilter(DesignMaterialsQueryTerms query)
    {
        return m => m.QueryTerms.CategoryId == query.CategoryId &&
                    m.QueryTerms.PageNumber <= query.PageNumber;
    }

    private static Func<DesignMaterial, bool> BuildUserFilter((string, string, string, string, string) arg)
    {
        var (name, brand, specifications, model, manufacturer) = arg;

        return m => m.Name.Contains(name) && m.Brand.Contains(brand) && m.Specifications.Contains(specifications) &&
                    m.Model.Contains(model) && m.Manufacturer.Contains(manufacturer);
    }
}