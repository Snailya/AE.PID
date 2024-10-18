using AE.PID.Core.DTOs;
using AE.PID.Visio.Core.Exceptions;

namespace AE.PID.Visio.Core.Interfaces;

public interface IDocumentUpdateService
{
    /// <summary>
    ///     Update the document content at the specified path by sending it to the server.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    /// <exception cref="NetworkNotValidException">There is a network error between server and local.</exception>
    Task UpdateAsync(string filePath);

    /// <summary>
    ///     Check if the library used by document is out of date.
    /// </summary>
    /// <param name="masters"></param>
    /// <returns></returns>
    /// <exception cref="NetworkNotValidException">There is a network error between server and local.</exception>
    bool HasUpdate(IEnumerable<MasterSnapshotDto> masters);
}