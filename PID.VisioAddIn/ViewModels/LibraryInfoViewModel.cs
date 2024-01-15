#nullable enable
using System;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class LibraryInfoViewModel : ReactiveObject
{
    private string _name = string.Empty;
    private Version? _localVersion;
    private Version? _remoteVersion;

    public LibraryInfoViewModel()
    {
        this.WhenAnyValue(
                x => x.LocalVersion,
                x => x.RemoteVersion
            )
            .Subscribe(_ => this.RaisePropertyChanged(nameof(NeedUpdate)));
    }

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public Version? LocalVersion
    {
        get => _localVersion;
        set => this.RaiseAndSetIfChanged(ref _localVersion, value);
    }

    public Version? RemoteVersion
    {
        get => _remoteVersion;
        set => this.RaiseAndSetIfChanged(ref _remoteVersion, value);
    }

    public bool NeedUpdate => _localVersion != null && _remoteVersion != null && _remoteVersion > _localVersion;
}