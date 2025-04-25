using System.ComponentModel;

namespace AE.PID.Server;

/// <summary>
///     管理员审计使用的模具DTO。
/// </summary>
internal class StencilAuditDto
{
    [Description("Id")] public int Id { get; set; }

    [Description("名称")] public string Name { get; set; } = string.Empty;

    [Description("创建时间")] public DateTime CreatedAt { get; set; }

    [Description("更新时间")] public DateTime? ModifiedAt { get; set; }

    [Description("最新快照")] public IEnumerable<StencilSnapshotAuditDto> LatestSnapshots { get; set; } = [];
}