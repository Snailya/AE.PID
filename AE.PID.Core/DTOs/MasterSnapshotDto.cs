namespace AE.PID.Core;

public class MasterSnapshotDto
{
    public string Name { get; set; } = string.Empty;
    public string BaseId { get; set; } = string.Empty;
    public string UniqueId { get; set; } = string.Empty;
    public string[] UniqueIdHistory { get; set; } = [];
}