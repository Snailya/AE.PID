using System;
using System.Reactive.Disposables;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class TreeNodeViewModelBase : ViewModelBase, IDisposable
{
    protected readonly CompositeDisposable CleanUp = new();
    private bool _isExpanded;
    private bool _isSelected;

    protected TreeNodeViewModelBase(int id, int depth, int parentId)
    {
        Id = id;
        Depth = depth;
        ParentId = parentId;
    }

    public int Id { get; }
    public int Depth { get; protected set; }
    public int ParentId { get; protected set; }

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
        CleanUp.Dispose();
    }
}