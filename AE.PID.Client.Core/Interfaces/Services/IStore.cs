using System;

namespace AE.PID.Client.Core;

/// <summary>
///     If the service need to cache the data before it is disposed, this interface should be implemented.
/// </summary>
public interface IStore : IDisposable
{
    /// <summary>
    ///     Save the data in the service.
    /// </summary>
    /// <returns></returns>
    void Save();
}