using ReactiveUI;

namespace AE.PID.ViewModels;

public class MasterViewModel : ReactiveObject
{
    private bool _isChecked;

    /// <summary>
    ///     The id of the master that used to get master from document.
    /// </summary>
    public string BaseId { get; set; }

    /// <summary>
    /// The user friendly name for the master.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Indicates whether is selected by user.
    /// </summary>
    public bool IsChecked
    {
        get => _isChecked;
        set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }
}