namespace PID.Server.Models;

/// <summary>
///     The request dto used for upload installer.
/// </summary>
public class UploadInstallerDto
{
        /// <summary>
        ///     The zip file for installer
        /// </summary>
        public IFormFile Installer { get; set; } = null!;

        /// <summary>
        ///     The version string of current uploaded app.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        ///     The description for the current version, might be something added, or updated.
        /// </summary>
        public string ReleaseNotes { get; set; } = string.Empty;
}