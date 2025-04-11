namespace AE.PID.Core;

public class MasterSnapshotDto
{
    public string Name { get; set; } = string.Empty;
    public string BaseId { get; set; } = string.Empty;
    public string UniqueId { get; set; } = string.Empty;
    public string[] UniqueIdHistory { get; set; } = [];
}

public class MasterSnapshotExtDto : MasterSnapshotDto
{
    public string LineStyle { get; set; } = string.Empty;
    public string FillStyle { get; set; } = string.Empty;
    public string TextStyle { get; set; } = string.Empty;
    public string MasterElement { get; set; } = string.Empty;
    public string MasterDocument { get; set; } = string.Empty;
}