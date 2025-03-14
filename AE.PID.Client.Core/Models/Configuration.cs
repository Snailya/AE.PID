using System;
using System.Collections.Generic;
using System.Linq;

namespace AE.PID.Client.Core;

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

    public object Clone()
    {
        return new Configuration
        {
            Server = Server,
            UserId = UserId,
            SkippedVersions = SkippedVersions,
            Stencils = Stencils.Select(x => (Stencil)x.Clone()).ToArray()
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

public class RuntimeConfiguration
{
    public string CompanyName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string UUID { get; set; } = string.Empty;

    public string InstallationPath { get; set; } = string.Empty;
    public string DataPath { get; set; } = string.Empty;
}