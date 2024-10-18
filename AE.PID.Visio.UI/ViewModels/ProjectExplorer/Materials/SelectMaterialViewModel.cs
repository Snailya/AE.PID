using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class SelectMaterialViewModel : ViewModelBase
{
    private readonly ReadOnlyObservableCollection<MaterialViewModel> _data;
    private readonly IMaterialService _materialService;
    private readonly ReadOnlyObservableCollection<MaterialCategoryViewModel> _validCategories;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isBusy;
    private string? _searchText;
    private MaterialCategoryViewModel? _selectedCategory;
    private MaterialViewModel? _selectedMaterial;

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

    public ReactiveCommand<Unit, MaterialViewModel?> Confirm { get; private set; }

    public MaterialCategoryViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
    }

    public MaterialViewModel? SelectedMaterial
    {
        get => _selectedMaterial;
        set => this.RaiseAndSetIfChanged(ref _selectedMaterial, value);
    }

    public ReadOnlyObservableCollection<MaterialCategoryViewModel> ValidCategories => _validCategories;
    public ReadOnlyObservableCollection<MaterialViewModel> Data => _data;

    public string Seed { get; set; } = string.Empty;

    public PageNavigatorViewModel PageNavigator { get; } = new(1, 15);
    public ReactiveCommand<Unit, Unit> Cancel { get; }

    private async Task<IEnumerable<MaterialViewModel>> LoadAsync(LoadCondition condition,
        PageRequest pageRequest)
    {
        IsBusy = true;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        var result = new List<MaterialViewModel>();

        if (condition.Id.HasValue)
        {
            var response = string.IsNullOrEmpty(condition.SearchText)
                ? await _materialService.GetAsync(condition.Id, pageRequest, cancellationToken)
                : await _materialService.SearchAsync(condition.SearchText!, condition.Id, pageRequest,
                    cancellationToken);

            result.AddRange(response.Items.Select(material => new MaterialViewModel(material)));

            PageNavigator.Update(response);
        }

        IsBusy = false;
        return result;
    }

    private class LoadCondition
    {
        public int? Id { get; set; }
        public string? SearchText { get; set; }
    }


    #region Constructors

    public SelectMaterialViewModel(IMaterialService materialMaterialService, string seed)
    {
        _materialService = materialMaterialService;

        Seed = seed;

        #region Commands

        Confirm = ReactiveCommand.Create(() => SelectedMaterial);
        Cancel = ReactiveCommand.Create(() => { });

        #endregion

        #region Subscriptions

        var categoryPredicate = Observable.StartAsync(async () => await materialMaterialService.GetCategoryMapAsync())
            .Select<Dictionary<string, string[]>, Func<Node<MaterialCategory, int>, bool>>(v =>
            {
                return node => v!.ContainsKey(Seed) ? v[Seed]!.Contains(node.Item.Code) : node.IsRoot;
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
            .Bind(out _validCategories)
            .DisposeMany()
            .Subscribe();

        // build local filter with search text
        var filter = this.WhenValueChanged(t => t.SearchText)
            .Throttle(TimeSpan.FromMilliseconds(400))
            .Select(BuildFilter);

        // whenever the selected category changed, reset to the first page.
        this.WhenAnyValue(x => x.SelectedCategory, x => x.SearchText)
            .Subscribe(_ => { PageNavigator.Reset(); });

        this.WhenAnyValue(x => x.PageNavigator.CurrentPage, x => x.PageNavigator.PageSize,
                (page, size) => new PageRequest(page, size)).CombineLatest(this.WhenAnyValue(x => x.SelectedCategory,
                x => x.SearchText,
                (category, searchText) => new LoadCondition { Id = category?.Id, SearchText = searchText }))
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

        Func<MaterialViewModel, bool> BuildFilter(string? searchText)
        {
            if (string.IsNullOrEmpty(searchText)) return _ => true;

            return material => JsonSerializer.Serialize(material).Contains(searchText);
        }
    }


    internal SelectMaterialViewModel()
    {
        // design
    }

    #endregion
}