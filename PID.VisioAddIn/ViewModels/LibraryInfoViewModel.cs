using System;
using System.Reactive.Disposables;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class LibraryInfoViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _cleanUp = new();
    private readonly ObservableAsPropertyHelper<bool> _needUpdate;
    private string _localVersion = string.Empty;

    private string _remoteVersion = string.Empty;

    public LibraryInfoViewModel()
    {
        this.WhenAnyValue(
                x => x.LocalVersion,
                x => x.RemoteVersion,
                (local, server) => !string.IsNullOrEmpty(server) &&
                                   (string.IsNullOrEmpty(local) || new Version(local) < new Version(server)))
            .ToProperty(this, x => x.NeedUpdate, out _needUpdate)
            .DisposeWith(_cleanUp);
    }

    public string Name { get; set; } = string.Empty;

    public string LocalVersion
    {
        get => _localVersion;
        set => this.RaiseAndSetIfChanged(ref _localVersion, value);
    }

    public string RemoteVersion
    {
        get => _remoteVersion;
        set => this.RaiseAndSetIfChanged(ref _remoteVersion, value);
    }

    public bool NeedUpdate => _needUpdate.Value;
    public int Id { get; set; }

    public void Dispose()
    {
        _cleanUp.Dispose();
    }
}