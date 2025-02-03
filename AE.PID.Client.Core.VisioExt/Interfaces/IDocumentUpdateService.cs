using AE.PID.Client.Core.VisioExt.Models;
using AE.PID.Core.DTOs;

namespace AE.PID.Client.Core.VisioExt;

public interface IDocumentUpdateService
{
    IObservable<bool> Initialized { get; }
    /// <summary>
    ///     Get the out of data symbols used by the documents.
    /// </summary>
    List<VisioMaster> GetOutdatedMasters { get; }

    /// <summary>
    ///     Update the document content at the specified path by sending it to the server.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="excludes">The unique id for the masters that need to be excluded when performing update.</param>
    /// <returns></returns>
    /// <exception cref="NetworkNotValidException">There is a network error between server and local.</exception>
    Task UpdateAsync(string filePath, string[]? excludes = null);

    /// <summary>
    ///     Check if the library used by document is out of date.
    /// </summary>
    /// <param name="localMasters"></param>
    /// <param name="excludes">The unique id for the masters that need to be excluded when performing update check.</param>
    /// <returns></returns>
    /// <exception cref="NetworkNotValidException">There is a network error between server and local.</exception>
    bool HasUpdate(IEnumerable<MasterSnapshotDto> localMasters, string[]? excludes = null);
}