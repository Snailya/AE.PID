using System.Reactive;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class OkCancelViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit>? Ok { get; set; }
    public ReactiveCommand<Unit, Unit>? Cancel { get; set; }
}