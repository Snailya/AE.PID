using AE.PID.Services;

namespace AE.PID.ViewModels;

public class ProgressValue
{
    public int Value { get; set; }
    public string Message { get; set; } = string.Empty;
    public TaskStatus Status { get; set; }
}