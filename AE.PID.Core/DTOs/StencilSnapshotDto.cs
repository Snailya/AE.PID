namespace AE.PID.Core.DTOs;

public class StencilSnapshotDto
{
    public int StencilId { get; set; }
    public string StencilName { get; set; }
    public string DownloadUrl { get; set; }
    public int Id { get; set; }

    // public OperationStatus  Status { get; set; }
}

public enum OperationStatus
{
    Added,
    Modified,
    Removed
}