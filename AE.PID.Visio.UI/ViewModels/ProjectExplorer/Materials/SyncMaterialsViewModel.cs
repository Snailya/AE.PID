using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class SyncMaterialsViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> Confirm { get; }
    public ReactiveCommand<Unit, Unit> Cancel { get; }

    #region Constructors

    public SyncMaterialsViewModel()
    {
        // Design
    }

    public SyncMaterialsViewModel(ReadOnlyObservableCollection<MaterialLocationViewModel> collection)
    {
        #region Commands

        Confirm = ReactiveCommand.CreateFromTask(async _ => { return Unit.Default; });

        #endregion
    }

    #endregion
}