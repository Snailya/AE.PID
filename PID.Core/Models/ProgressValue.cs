namespace AE.PID.Core.Models;

public class ProgressValue
{
    public double Value { get; set; }
    public string Message { get; set; } = string.Empty;
    public TaskStatus Status { get; set; }
}