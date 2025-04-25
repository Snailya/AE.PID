using System.ComponentModel;

namespace AE.PID.Core;

public class MasterSnapshotDto
{
    [Description("形状名称")] public string Name { get; set; } = string.Empty;
    [Description("BaseID")] public string BaseId { get; set; } = string.Empty;
    [Description("UnqiueID")] public string UniqueId { get; set; } = string.Empty;
    [Description("历史UniqueID")] public string[] UniqueIdHistory { get; set; } = [];
}