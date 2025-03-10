using System.Threading.Tasks;

namespace AE.PID.Client.Core;

/// <summary>
///     The service provide ways to check if there is a valid update and offer re-download if the installer is broken.
/// </summary>
public interface IAppUpdateService
{
    /// <summary>
    ///     Compare the version with server's latest application version to determine if there is a valid update.
    /// </summary>
    /// <returns>
    ///     An object that represents the application info if there is an update. null if there is no update.
    /// </returns>
    /// <exception cref="NetworkNotValidException">There is a network error between server and local.</exception>
    Task<PendingAppUpdate?> CheckUpdateAsync(string version);

    /// <summary>
    ///     Download the installer from the specified url if it is not exist on local storage.
    /// </summary>
    /// <param name="downloadUrl"></param>
    /// <returns>The local storage path of the downloaded installer</returns>
    /// <exception cref="NetworkNotValidException">There is a network error between server and local.</exception>
    Task<string> DownloadAsync(string downloadUrl);

    // /// <summary>
    // ///     Save the pending update to configuration so that it could be resume on next time start.
    // /// </summary>
    // void SuspendUpdate();

    /// <summary>
    ///     Create a new process to execute the installer.
    /// </summary>
    /// <param name="executablePath" />
    /// <exception cref="FileExtensionNotSupportException">The installer is not supported.</exception>
    Task InstallAsync(string executablePath);
}