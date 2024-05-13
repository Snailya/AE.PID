using System.Collections.ObjectModel;
using System.Linq;
using AE.PID.Dtos;
using AE.PID.Tools;
using ReactiveUI;

namespace AE.PID.Models;

public class ReactiveLibrary : ReactiveObject
{
    private string _version = string.Empty;

    /// <summary>
    ///     The identifier used in server request.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     The name of the library, used for user identification.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     The named version, notice that even the version is correct, the file may not be the same as the server as user
    ///     might edit the local file which is not as expected.
    /// </summary>
    public string Version
    {
        get => _version;
        set => this.RaiseAndSetIfChanged(ref _version, value);
    }

    /// <summary>
    ///     The hash of the server file used for checking if user edit the local file. The app should prompt user to notice
    ///     that the local version is not the same as the server.
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    ///     The path of the local file which used to load the local file to active document when user click library button in
    ///     the ribbon, and to persist file download from server.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    ///     Then content of the library.
    /// </summary>
    public ObservableCollection<LibraryItem> Items { get; set; } = new([]);

    public static ReactiveLibrary FromLibrary(Library library)
    {
        return new ReactiveLibrary
        {
            Id = library.Id,
            Name = library.Name,
            Version = library.Version,
            Hash = library.Hash,
            Path = library.Path,
            Items = new ObservableCollection<LibraryItem>(library.Items)
        };
    }

    public static ReactiveLibrary FromLibraryDto(LibraryDto dto)
    {
        return new ReactiveLibrary
        {
            Id = dto.Id,
            Name = dto.Name,
            Version = dto.Version,
            Path = System.IO.Path.ChangeExtension(
                System.IO.Path.Combine(Constants.LibraryFolder, dto.Name),
                "vssx"),
            Items = new ObservableCollection<LibraryItem>(dto.Items.Select(LibraryItem.FromLibraryItemDto))
        };
    }
}