namespace AE.PID.Server.Data;

/// <summary>
/// </summary>
public class StencilSnapshot : EntityBase
{
    /// <summary>
    ///     存储Library的物理文件
    /// </summary>
    public string PhysicalFilePath { get; set; }

    public string Description { get; set; }

    /// <summary>
    ///     草稿状态、发布状态、废止状态
    /// </summary>
    public SnapshotStatus Status { get; set; }

    #region -- Navigation Properties --

    public int StencilId { get; set; }
    public Stencil Stencil { get; set; }

    /// <summary>
    ///     多对多
    /// </summary>
    public ICollection<MasterContentSnapshot> MasterContentSnapshots { get; set; } = [];

    #endregion
}