using System.Reactive;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia.Shared;

public class SimpleDialogViewModel(string message, string? title) : ViewModelBase
{
    public ReactiveCommand<Unit, bool> Confirm { get; set; } = ReactiveCommand.Create(() => true);
    public ReactiveCommand<Unit, bool> Cancel { get; set; } = ReactiveCommand.Create(() => false);

    public string Message { get; set; } = message;
    public string Title { get; set; } = title ?? string.Empty;
}