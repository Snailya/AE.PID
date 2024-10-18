namespace AE.PID.Visio.Core.Interfaces;

public interface IStore : IDisposable
{
    /// <summary>
    ///     Save the data in the service.
    /// </summary>
    /// <returns></returns>
    void Save();
}