using System.Reactive;
using System.Threading;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class TaskProgressViewModel(CancellationTokenSource cts) : ReactiveObject
{
    private int _current;

    public int Current
    {
        get => _current;
        set => this.RaiseAndSetIfChanged(ref _current, value);
    }

    public ReactiveCommand<Unit, Unit> Cancel { get; } = ReactiveCommand.Create(cts.Cancel);
}