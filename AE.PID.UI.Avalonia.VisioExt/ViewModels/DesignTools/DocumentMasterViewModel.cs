using AE.PID.Client.Core.VisioExt.Models;
using ReactiveUI;

namespace AE.PID.UI.Avalonia.VisioExt;

public class DocumentMasterViewModel : ReactiveObject
{
    private bool _isSelected;

    public DocumentMasterViewModel(VisioMaster master)
    {
        Source = master;
        Name = master.Name;
        BaseId = master.Id.BaseId;
        UniqueId = master.Id.UniqueId;
    }

    public DocumentMasterViewModel()
    {
    }

    public VisioMaster Source { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    public string Name { get; set; }

    public string BaseId { get; set; }
    public string UniqueId { get; set; }
}