using System.Collections;
using System.Reactive;
using AE.PID.Client.UI.Avalonia.Shared;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public class SyncMaterialsViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> Confirm { get; set; }
    public ReactiveCommand<Unit, Unit> Cancel { get; set; }
    
    #region Constructors
    
    public SyncMaterialsViewModel(ICollection collection)
    {
    }

    #endregion
}