using System.Reactive;
using System.Reactive.Linq;
using AE.PID.Client.Core.VisioExt;
using AE.PID.Client.UI.Avalonia.Shared;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia.VisioExt;

public class ConfirmUpdateDocumentWindowViewModel : ViewModelBase
{
    private readonly ObservableAsPropertyHelper<bool> _allSelected;

    public ObservableCollectionExtended<DocumentMasterViewModel> DocumentMasters { get; }

    public bool AllSelected => _allSelected.Value;

    #region -- Commands --

    public ReactiveCommand<Unit, VisioMaster[]> Confirm { get; set; }

    public ReactiveCommand<Unit, Unit> ToggleSelectAll { get; set; }

    public ReactiveCommand<Unit, Unit> Cancel { get; set; }

    #endregion

    #region -- Constructors --

    public ConfirmUpdateDocumentWindowViewModel(DocumentMasterViewModel[] symbols)
    {
        DocumentMasters = new ObservableCollectionExtended<DocumentMasterViewModel>(symbols.OrderBy(x => x.Name));

        var observeMasters = DocumentMasters.ToObservableChangeSet()
            .AutoRefresh(x => x.IsSelected);

        var canConfirm = observeMasters.ToCollection().Select(x => x.Any(i => i.IsSelected));
        Confirm = ReactiveCommand.Create(
            () =>
            {
                var excludes = DocumentMasters.Where(x => x.IsSelected)
                    .Select(x => x.Source).ToArray();
                return excludes;
            }, canConfirm);

        Cancel = ReactiveCommand.Create(() => { });

        ToggleSelectAll = ReactiveCommand.Create(() =>
        {
            if (AllSelected)
                foreach (var symbol in DocumentMasters)
                    symbol.IsSelected = false;
            else
                foreach (var symbol in DocumentMasters.Where(x => !x.IsSelected).ToList())
                    symbol.IsSelected = true;
        });

        observeMasters
            .ToCollection()
            .Select(x => x.All(i => i.IsSelected))
            .ToProperty(this, x => x.AllSelected, out _allSelected);
    }

    internal ConfirmUpdateDocumentWindowViewModel()
    {
        // Design only
    }

    #endregion
}