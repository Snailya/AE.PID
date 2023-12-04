using ReactiveUI;

namespace AE.PID.ViewModels;

public class SelectByMasterOptionViewModel : MasterViewModel
{
    /// <summary>
    ///     The target file path.
    /// </summary>
    public string Path { get; set; }
}

public class MasterViewModel : ReactiveObject
{
    private bool _isChecked;

    /// <summary>
    ///     The id of the master that used to get master from document.
    /// </summary>
    public string BaseId { get; set; }

    public string Name { get; set; }

    public bool IsChecked
    {
        get => _isChecked;
        set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }
}