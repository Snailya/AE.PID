using AE.PID.Services;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class ProgressPageViewModel : ViewModelBase
{
    private ProgressValue _progressValue = new() { Value = 0, Status = TaskStatus.Created, Message = string.Empty };
    public bool IsIndeterminate { get; set; }

    public ProgressValue ProgressValue
    {
        get => _progressValue;
        set => this.RaiseAndSetIfChanged(ref _progressValue, value);
    }
}