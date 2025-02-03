using System.Collections;
using System.Collections.ObjectModel;
using System.Reactive;
using AE.PID.UI.Shared;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public class SyncMaterialsViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> Confirm { get; }
    public ReactiveCommand<Unit, Unit> Cancel { get; }

    #region Constructors

    public SyncMaterialsViewModel()
    {
        // Design
    }

    public SyncMaterialsViewModel(ICollection collection)
    {
        #region Commands

        Confirm = ReactiveCommand.CreateFromTask(async _ => { return Unit.Default; });

        #endregion
    }

    #endregion
}