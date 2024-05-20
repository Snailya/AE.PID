using ReactiveUI;

namespace AE.PID.ViewModels;

public class SelectableViewModel<T>(T source) : ReactiveObject
{
    private bool _isSelected;
    public T Source { get; set; } = source;

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }
}