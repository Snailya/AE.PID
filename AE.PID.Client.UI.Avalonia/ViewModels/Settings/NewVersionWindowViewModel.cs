using System.Reactive;
using AE.PID.Client.UI.Avalonia.Shared;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia;

public class NewVersionWindowViewModel : WindowViewModelBase
{
    public string ReleaseNotes { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public ReactiveCommand<Unit, bool> Confirm { get; } = ReactiveCommand.Create(() => true);
    public ReactiveCommand<Unit, bool> Cancel { get; set; } = ReactiveCommand.Create(() => false);
}