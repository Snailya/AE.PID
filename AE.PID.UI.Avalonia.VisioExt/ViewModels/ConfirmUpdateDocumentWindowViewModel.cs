using System.Collections.ObjectModel;
using System.Reactive;
using AE.PID.UI.Shared;
using ReactiveUI;

namespace AE.PID.UI.Avalonia.VisioExt;

public class ConfirmUpdateDocumentWindowViewModel : ViewModelBase
{
    public ConfirmUpdateDocumentWindowViewModel(SymbolViewModel[] symbols)
    {
        Symbols = new ObservableCollection<SymbolViewModel>(symbols);

        Confirm = ReactiveCommand.Create(
            () =>
            {
                var excludes = Symbols.Where(x => !x.IsSelected).Select(x => x.UniqueId).ToArray();
                return excludes;
            });

        Cancel = ReactiveCommand.Create(() => { });
    }

    public ReactiveCommand<Unit, string[]> Confirm { get; set; }
    public ReactiveCommand<Unit, Unit> Cancel { get; set; }

    public ObservableCollection<SymbolViewModel> Symbols { get; }

    protected override void SetupStart()
    {
        base.SetupStart();

        // _toolService.Load();
    }
}