using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class SyncFunctionGroupViewModel(FunctionViewModel? remote, FunctionViewModel? local) : ReactiveObject
{
    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    /// <summary>
    ///     The local version of the function.
    /// </summary>
    public FunctionViewModel? Local { get; } = local;

    /// <summary>
    ///     The remote version of the function.
    /// </summary>
    public FunctionViewModel? Remote { get; } = remote;

    public SyncStatus Status =>
        Remote is null ? SyncStatus.Added :
        Local is null ? SyncStatus.Deleted :
        Local.Equals(Remote) ? SyncStatus.Unchanged : SyncStatus.Modified;

    /// <summary>
    ///     The user oriented name.
    /// </summary>
    public string Name => $"{Remote?.Name} : {Local?.Name}";
}