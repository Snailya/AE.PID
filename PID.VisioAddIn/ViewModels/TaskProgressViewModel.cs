using System.Diagnostics;
using System.Reactive;
using System.Threading;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class TaskProgressViewModel : ReactiveObject
{
    private readonly CancellationTokenSource _cts;
    private int _current;

    public TaskProgressViewModel(CancellationTokenSource cts)
    {
        _cts = cts;
        Cancel = ReactiveCommand.Create(() =>
            {
                _cts.Cancel();
                Debug.WriteLine("Canceled");
            }
        );
    }

    public int Current
    {
        get => _current;
        set => this.RaiseAndSetIfChanged(ref _current, value);
    }

    public ReactiveCommand<Unit, Unit> Cancel { get; }
}