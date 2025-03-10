using System.Reactive;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia.Shared;

public abstract class DialogViewModelBase : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> Confirm { get; protected set; } = ReactiveCommand.Create(() => { });
    public ReactiveCommand<Unit, Unit> Cancel { get; protected set; } = ReactiveCommand.Create(() => { });
}