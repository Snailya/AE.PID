using System.Reactive;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class NewVersionViewModel : ViewModelBase
{
    public string ReleaseNotes { get; set; }
    public string Version { get; set; }
    public ReactiveCommand<Unit, bool> Confirm { get; } = ReactiveCommand.Create(() => true);
    public ReactiveCommand<Unit, bool> Cancel { get; set; } = ReactiveCommand.Create(() => false);
}