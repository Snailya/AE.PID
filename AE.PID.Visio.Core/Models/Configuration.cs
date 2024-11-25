namespace AE.PID.Visio.Core.Models;

public class Configuration : ICloneable
{
    /// <summary>
    ///     The backend server address
    /// </summary>
    public string Server { get; set; } = "http://172.18.168.35:32769";

    /// <summary>
    ///     The user id used as operator id for PDMS request.
    /// </summary>
    public string UserId { get; set; } = "";

    /// <summary>
    ///     The update version to skip
    /// </summary>
    public string[] SkippedVersions { get; set; } = [];

    /// <summary>
    ///     The identifier hash for the current libraries.
    /// </summary>
    public IEnumerable<Stencil> Stencils { get; set; } = [];

    /// <summary>
    ///     The update pending to download or install.
    /// </summary>
    public PendingAppUpdate? PendingAppUpdate { get; set; }

    public object Clone()
    {
        return new Configuration
        {
            Server = Server,
            UserId = UserId,
            SkippedVersions = SkippedVersions,
            Stencils = Stencils.Select(x => (Stencil)x.Clone()).ToArray(),
            PendingAppUpdate = PendingAppUpdate is { } update ? (PendingAppUpdate)update.Clone() : null
        };
    }
}

public class Stencil : ICloneable
{
    /// <summary>
    ///     The id for the stencil snapshot, used to compare with server to decide whether it is out-of-date.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     The name of the stencil
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     The physical file path that the snapshot stands for.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    public object Clone()
    {
        return new Stencil
        {
            Id = Id,
            Name = Name,
            FilePath = FilePath
        };
    }
}

public class PendingAppUpdate : ICloneable
{
    /// <summary>
    ///     The version of pending update.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    ///     The description of updated features.
    /// </summary>
    public string ReleaseNotes { get; set; } = string.Empty;

    /// <summary>
    ///     The download url for the installer.
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    ///     Indicates whether the installer has been saved
    /// </summary>
    public bool IsDownloaded { get; set; }

    /// <summary>
    ///     The executable path.
    /// </summary>
    public string InstallerPath { get; set; } = string.Empty;

    public object Clone()
    {
        return new PendingAppUpdate
        {
            Version = Version,
            InstallerPath = InstallerPath,
            IsDownloaded = IsDownloaded,
            DownloadUrl = DownloadUrl
        };
    }
}

public class RuntimeConfiguration
{
    public string ProductName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string UUID { get; set; } = string.Empty;

    public string AppDataFolder { get; set; } = string.Empty;
}