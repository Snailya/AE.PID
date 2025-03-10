using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public class SyncFunctionGroupViewModel : ReactiveObject
{
    private bool _isSelected;
    private bool _useRemote;

    /// <summary>
    ///     Flag for whether to use the remote value to replace the local one.
    /// </summary>
    public bool UseRemote
    {
        get => _useRemote;
        set => this.RaiseAndSetIfChanged(ref _useRemote, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    /// <summary>
    ///     The local version of the function.
    /// </summary>
    public FunctionViewModel? Local { get; set; }

    /// <summary>
    ///     The remote version of the function.
    /// </summary>
    public FunctionViewModel? Remote { get; set; }

    public SyncStatus Status =>
        Remote is null ? SyncStatus.Added :
        Local is null ? SyncStatus.Deleted :
        Local.Equals(Remote) ? SyncStatus.Unchanged : SyncStatus.Modified;

    /// <summary>
    ///     The user oriented name.
    /// </summary>
    public string Name => $"{Remote?.Name} : {Local?.Name}";
}