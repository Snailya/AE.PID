using System;
using System.Reactive.Disposables;
using AE.PID.Models;
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
    }

    public LibraryInfoViewModel(ReactiveLibrary library)
    {
        Id = library.Id;
        Name = library.Name;

        library.WhenAnyValue(x => x.Version)
            .BindTo(this, x => x.LocalVersion)
            .DisposeWith(_cleanUp);

        this.WhenAnyValue(
                x => x.LocalVersion,
                x => x.RemoteVersion,
                (local, server) => !string.IsNullOrEmpty(server) &&
                                   (string.IsNullOrEmpty(local) || new Version(local) < new Version(server)))
            .ToProperty(this, x => x.NeedUpdate, out _needUpdate)
            .DisposeWith(_cleanUp);
    }

    public string Name { get; set; }

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
    public int Id { get; private set; }

    public void Dispose()
    {
        _cleanUp.Dispose();
    }
}