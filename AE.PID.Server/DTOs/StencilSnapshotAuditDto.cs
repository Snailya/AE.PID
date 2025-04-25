using System.ComponentModel;
using AE.PID.Server.Data;

namespace AE.PID.Server;

/// <summary>
///     管理员审计使用的模具快照DTO
/// </summary>
internal class StencilSnapshotAuditDto
{
    [property: Description("Id")]
    public int Id { get; set; }
    
    [property: Description("描述")]
    public string Description { get; set; } = string.Empty;
    
    [property: Description("创建时间")]
    public DateTime CreatedAt { get; set; }
    
    [property: Description("更新时间")]
    public DateTime? ModifiedAt { get; set; }
    
    [property: Description("状态")]
    public SnapshotStatus Status { get; set; }
}