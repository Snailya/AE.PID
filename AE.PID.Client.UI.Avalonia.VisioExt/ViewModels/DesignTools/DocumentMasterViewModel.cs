using AE.PID.Client.Core.VisioExt;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia.VisioExt;

public class DocumentMasterViewModel(VisioMaster master) : ReactiveObject
{
    private bool _isSelected;

    public VisioMaster Source { get; } = master;

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    /// <summary>
    ///     The name of the master in Visio
    /// </summary>
    public string Name { get; set; } = master.Name;

    /// <summary>
    ///     The base id of the master in Visio. The base id will never change unless update it manually.
    /// </summary>
    public string BaseId { get; set; } = master.Id.BaseId;

    /// <summary>
    ///     The unique id of the master in Visio, can be used as identifier for master.
    /// </summary>
    public string UniqueId { get; set; } = master.Id.UniqueId;
}