using AE.PID.Client.Core;
using AE.PID.UI.Shared;

namespace AE.PID.Client.UI.Avalonia;

public class ProjectViewModel(Project? project) : ViewModelBase
{
    public ProjectViewModel(ResolveResult<Project?> resolve) : this(resolve.Value)
    {
        ResolveFrom = resolve.ResolveFrom;
    }

    public Project? Source { get; } = project;

    /// <summary>
    ///     The id of the project, 0 for no project.
    /// </summary>
    public int Id { get; set; } = project?.Id ?? 0;

    /// <summary>
    ///     The user-oriented code of the project.
    /// </summary>
    public string Code { get; set; } = project?.Code ?? string.Empty;

    /// <summary>
    ///     The name of the project;
    /// </summary>
    public string Name { get; set; } = project?.Name ?? string.Empty;

    /// <summary>
    ///     The shortname of the project
    /// </summary>
    public string FamilyName { get; set; } = project?.FamilyName ?? string.Empty;

    /// <summary>
    ///     The data from where this project value comes from.
    /// </summary>
    public DataSource ResolveFrom { get; set; }
}