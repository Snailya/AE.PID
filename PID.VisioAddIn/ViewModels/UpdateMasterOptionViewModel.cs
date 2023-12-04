using ReactiveUI;

namespace AE.PID.ViewModels;

public class UpdateMasterOptionViewModel : ReactiveObject
{
    private bool _isChecked;

    /// <summary>
    ///     The unique id of the master that used to get master from document.
    /// </summary>
    public int SourceId { get; set; }

    /// <summary>
    ///     The name of the master that displayed in the ui as user identifier.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Identifies that the master is selected by user to let update.
    /// </summary>
    public bool IsChecked
    {
        get => _isChecked;
        set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }

    /// <summary>
    ///     The unique id in library file that used to get the latest version fo the master.
    /// </summary>
    public string TargetId { get; set; }

    /// <summary>
    ///     The name of the library that to locate which file to open.
    /// </summary>
    public string LibraryName { get; set; }
}