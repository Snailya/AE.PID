using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using AE.PID.Core.Models;
using AE.PID.UI.Shared;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public class StandardMaterialViewModel : ViewModelBase
{
    private readonly ReadOnlyObservableCollection<MaterialViewModel> _data;

    private readonly ReadOnlyObservableCollection<MaterialCategoryViewModel> _filteredCategories;
    private readonly IMaterialService _materialService;

    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isBusy;
    private string? _searchText;
    private MaterialCategoryViewModel? _selectedCategory;
    private MaterialViewModel? _selectedData;

    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    public string? SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public ReactiveCommand<Unit, MaterialViewModel> Confirm { get; private set; }
    public ReactiveCommand<Unit, Unit> Cancel { get; }

    public MaterialCategoryViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
    }

    /// <summary>
    ///     The material in the standard collection that the user has selected
    /// </summary>
    public MaterialViewModel? SelectedData
    {
        get => _selectedData;
        set => this.RaiseAndSetIfChanged(ref _selectedData, value);
    }

    /// <summary>
    ///     The filtered categories that matches this material location type.
    /// </summary>
    public ReadOnlyObservableCollection<MaterialCategoryViewModel> FilteredCategories => _filteredCategories;

    /// <summary>
    ///     The materials that matches the current page and conditions.
    /// </summary>
    public ReadOnlyObservableCollection<MaterialViewModel> Data => _data;

    /// <summary>
    ///     The type of the material location, this value is used to filter the material category.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    public PageNavigatorViewModel PageNavigator { get; } = new(1, 15);


    private async Task<IEnumerable<MaterialViewModel>> LoadAsync((int? CategoryId, string? SearchText) condition,
        PageRequest pageRequest)
    {
        IsBusy = true;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        var result = new List<MaterialViewModel>();

        if (condition.CategoryId.HasValue)
        {
            var response = string.IsNullOrEmpty(condition.SearchText)
                ? await _materialService.GetAsync(condition.CategoryId, pageRequest, cancellationToken)
                : await _materialService.SearchAsync(condition.SearchText!, condition.CategoryId, pageRequest,
                    cancellationToken);

            result.AddRange(response.Items.Select(material => new MaterialViewModel(material)));

            PageNavigator.Update(response);
        }

        IsBusy = false;
        return result;
    }

    #region -- Constructors --

    public StandardMaterialViewModel(NotificationHelper notificationHelper, IMaterialService materialService,
        MaterialLocationContext context)
    {
        _materialService = materialService;
        
        Type = context.MaterialLocationType;

        #region Commands

        var canConfirm = this.WhenAnyValue(x => x.SelectedData)
            .Select(x => x != null);
        Confirm = ReactiveCommand.Create(() =>
        {
            // whenever user select material, send the feedback to the server. no need to wait.

            materialService.FeedbackAsync(context, SelectedData!.Source.Id);
            return SelectedData;
        }, canConfirm);
        Cancel = ReactiveCommand.Create(() => { });

        #endregion

        #region Subscriptions

        var categoryPredicate = Observable.StartAsync(async () => await materialService.GetCategoryMapAsync())
            .Select<Dictionary<string, string[]>, Func<Node<MaterialCategory, int>, bool>>(v =>
            {
                return node => v!.ContainsKey(Type) ? v[Type]!.Contains(node.Item.Code) : node.IsRoot;
            });

        // load the categories from task
        ObservableChangeSet.Create<MaterialCategory, int>(async cache =>
            {
                var categories = await _materialService.GetCategoriesAsync();
                cache.AddOrUpdate(categories);

                return () => { };
            }, t => t.Id)
            .TransformToTree(x => x.ParentId, categoryPredicate)
            .Transform(node => new MaterialCategoryViewModel(node))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _filteredCategories)
            .DisposeMany()
            .Subscribe();

        // build local filter with search text
        var filter = this.WhenValueChanged(t => t.SearchText)
            .Throttle<string>(TimeSpan.FromMilliseconds(400))
            .Select(BuildFilter);

        // whenever the selected category changed, reset to the first page.
        this.WhenAnyValue(x => x.SelectedCategory, x => x.SearchText)
            .Subscribe(_ => { PageNavigator.Reset(); });

        this.WhenAnyValue(x => x.PageNavigator.CurrentPage, x => x.PageNavigator.PageSize,
                (page, size) => new PageRequest(page, size)).CombineLatest(this.WhenAnyValue(x => x.SelectedCategory,
                x => x.SearchText,
                (category, searchText) => new ValueTuple<int?, string?>(category?.Id, searchText)))
            .Select(x =>
                ObservableChangeSet.Create<MaterialViewModel>(async list =>
                {
                    var items = await LoadAsync(x.Second, x.First);
                    list.AddRange(items);
                    return () => { };
                }))
            // repeatedly reload data using Dynamic Data's switch operator which will clear previous data and add newly loaded data
            .Switch()
            .Filter(filter)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _data)
            .Subscribe();

        #endregion

        return;

        Func<MaterialViewModel, bool> BuildFilter(string? searchText)
        {
            if (string.IsNullOrEmpty(searchText)) return _ => true;

            return material => JsonSerializer.Serialize(material).Contains(searchText);
        }
    }


    internal StandardMaterialViewModel()
    {
        // design
    }

    #endregion
}