using ReactiveUI;

namespace AE.PID.ViewModels.Components;

public class OkCancelFeedbackViewModel : OkCancelViewModel
{
    private string _message = string.Empty;

    public string Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged(ref _message, value);
    }
}