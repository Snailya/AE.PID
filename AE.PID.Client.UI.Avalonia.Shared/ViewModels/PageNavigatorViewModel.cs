using System.Reactive;
using DynamicData.Binding;
using DynamicData.Operators;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia.Shared;

public class PageNavigatorViewModel : AbstractNotifyPropertyChanged
{
    private int _currentPage;
    private int _pageCount;
    private int _pageSize;
    private int _totalCount;

    public PageNavigatorViewModel(int currentPage, int pageSize)
    {
        _currentPage = currentPage;
        _pageSize = pageSize;

        var canGoNextPage = this.WhenAnyValue(x => x.CurrentPage, x => x.PageCount,
            (current, pageCount) => current < pageCount);
        GoNextPage = ReactiveCommand.Create(() => { CurrentPage += 1; },
            canGoNextPage);
        var canGoPreviousPage = this.WhenAnyValue(x => x.CurrentPage, current => current > 1);
        GoPreviousPage =
            ReactiveCommand.Create(() => { CurrentPage -= 1; }, canGoPreviousPage);
    }

    internal PageNavigatorViewModel()
    {
        // Design
    }

    public ReactiveCommand<Unit, Unit> GoNextPage { get; set; }

    public ReactiveCommand<Unit, Unit> GoPreviousPage { get; set; }

    public int TotalCount
    {
        get => _totalCount;
        private set => SetAndRaise(ref _totalCount, value);
    }

    public int PageCount
    {
        get => _pageCount;
        private set => SetAndRaise(ref _pageCount, value);
    }

    public int CurrentPage
    {
        get => _currentPage;
        private set => SetAndRaise(ref _currentPage, value);
    }


    public int PageSize
    {
        get => _pageSize;
        private set => SetAndRaise(ref _pageSize, value);
    }


    public void Update(IPageResponse response)
    {
        CurrentPage = response.Page;
        PageSize = response.PageSize;
        PageCount = response.Pages;
        TotalCount = response.TotalSize;
    }

    public void Reset()
    {
        CurrentPage = 1;
        PageCount = 0;
        TotalCount = 0;
    }
}