using System.Reactive;
using AE.PID.UI.Shared;
using ReactiveUI;

namespace AE.PID.UI.Avalonia.ViewModels;

public class NewVersionViewModel : ViewModelBase
{
    public string ReleaseNotes { get; set; }
    public string Version { get; set; }
    public ReactiveCommand<Unit, bool> Confirm { get; } = ReactiveCommand.Create(() => true);
    public ReactiveCommand<Unit, bool> Cancel { get; set; } = ReactiveCommand.Create(() => false);
}