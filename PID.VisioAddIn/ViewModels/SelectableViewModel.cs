using ReactiveUI;

namespace AE.PID.ViewModels;

public class SelectableViewModel<T>(T source) : ReactiveObject
{
    public T Source { get; set; } = source;

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }
}