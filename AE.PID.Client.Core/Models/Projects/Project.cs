namespace AE.PID.Client.Core;

public class Project
{
    /// <summary>
    ///     The PDMS related id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     The name of the project.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     The code of the project.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    ///     The short name of the project.
    /// </summary>
    public string FamilyName { get; set; } = string.Empty;

    /// <summary>
    ///     The top-level leader of the project.
    /// </summary>
    public string Director { get; set; } = string.Empty;

    /// <summary>
    ///     The project manager of the project.
    /// </summary>
    public string ProjectManager { get; set; } = string.Empty;
}