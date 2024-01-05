using System;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class LibraryViewModel : ReactiveObject
{
    private string _name;
    private Version _localVersion;
    private Version _remoteVersion;

    public LibraryViewModel()
    {
        this.WhenAnyValue(
                x => x.LocalVersion,
                x => x.RemoteVersion)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(NeedUpdate)));
    }

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public Version LocalVersion
    {
        get => _localVersion;
        set => this.RaiseAndSetIfChanged(ref _localVersion, value);
    }

    public Version RemoteVersion
    {
        get => _remoteVersion;
        set => this.RaiseAndSetIfChanged(ref _remoteVersion, value);
    }

    public bool NeedUpdate => _remoteVersion > _localVersion;
}