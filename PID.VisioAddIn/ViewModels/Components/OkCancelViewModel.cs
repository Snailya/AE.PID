using System.Reactive;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class OkCancelViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit>? Ok { get; set; }
    public ReactiveCommand<Unit, Unit>? Cancel { get; set; }
}

public class OkCancelFeedbackViewModel : OkCancelViewModel
{
    private string _message = string.Empty;

    public string Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged(ref _message, value);
    }
}