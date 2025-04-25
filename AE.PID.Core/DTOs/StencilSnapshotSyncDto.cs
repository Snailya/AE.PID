using System.ComponentModel;

namespace AE.PID.Core;

/// <summary>
///     客户端同步使用的模具快照DTO。
/// </summary>
public class StencilSnapshotSyncDto
{
    [Description("模具ID")] public int StencilId { get; set; }
    [Description("快照ID")] public int Id { get; set; }
    [Description("模具名称")] public string StencilName { get; set; } = string.Empty;
    [Description("下载链接")] public string DownloadUrl { get; set; } = string.Empty;
    [Description("快照描述")] public string Description { get; set; } = string.Empty;
}