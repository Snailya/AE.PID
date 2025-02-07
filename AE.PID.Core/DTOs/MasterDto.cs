namespace AE.PID.Core.DTOs;

public class MasterDto
{
    /// <summary>
    ///     模具的名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     模具的BaseID，用来判断与库中的哪个模具关联
    /// </summary>
    public string BaseId { get; set; }

    /// <summary>
    ///     模具的UniqueID，用来定位文档模具。
    /// </summary>
    public string UniqueId { get; set; }
}