using AE.PID.Client.Core.VisioExt.Models;
using ReactiveUI;

namespace AE.PID.UI.Avalonia.VisioExt;

public class SymbolViewModel : ReactiveObject
{
    private bool _isSelected;

    public SymbolViewModel(VisioMaster symbol)
    {
        Source = symbol;
        Name = symbol.Name;
        UniqueId = symbol.Id.UniqueId;
    }

    public SymbolViewModel()
    {
    }

    public VisioMaster Source { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    public string Name { get; set; }
    public string UniqueId { get; set; }
}