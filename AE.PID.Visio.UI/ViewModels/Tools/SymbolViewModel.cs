using AE.PID.Visio.Core.Models;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class SymbolViewModel(Symbol symbol) : ReactiveObject
{
    private bool _isSelected;
    public Symbol Source { get; } = symbol;

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    public string Id { get; set; } = symbol.Id;
    public string Name { get; set; } = symbol.Name;
}