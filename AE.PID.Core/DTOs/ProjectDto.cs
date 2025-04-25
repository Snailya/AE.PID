using System.ComponentModel;

namespace AE.PID.Core;

public class ProjectDto
{
    [Description("ID")] public int Id { get; set; }
    [Description("项目名称")] public string Name { get; set; } = string.Empty;
    [Description("项目编码")] public string Code { get; set; } = string.Empty;
    [Description("项目简称")] public string FamilyName { get; set; } = string.Empty;
}