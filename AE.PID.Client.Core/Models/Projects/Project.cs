namespace AE.PID.Client.Core;

public class Project
{
    /// <summary>
    ///     The PDMS related id.
    /// </summary>
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public string FamilyName { get; set; } = string.Empty;

    public string Director { get; set; } = string.Empty;
    public string ProjectManager { get; set; } = string.Empty;
}