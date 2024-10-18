using System.ComponentModel.DataAnnotations;

namespace AE.PID.Server.Data;

public class MasterContentSnapshot : EntityBase
{
    public SnapshotStatus Status { get; set; }

    [Required] public string BaseId { get; set; }

    [Required] public string UniqueId { get; set; }

    /// <summary>
    ///     The line style that applies on the item.
    /// </summary>
    public string LineStyleName { get; set; }

    /// <summary>
    ///     The fill style that applies on the item.
    /// </summary>
    public string FillStyleName { get; set; }

    /// <summary>
    ///     The text style that applies on the item.
    /// </summary>
    public string TextStyleName { get; set; }

    /// <summary>
    ///     The string of XElement that stands for Master Element in /visio/masters/maters.xml
    /// </summary>
    [Required]
    public string MasterElement { get; set; }

    /// <summary>
    ///     The string of XDocument that stands for /visio/masters/master{i}.xml
    /// </summary>
    [Required]
    public string MasterDocument { get; set; }

    #region -- Navigation Properties --

    public int MasterId { get; set; }
    public Master Master { get; set; }

    public ICollection<StencilSnapshot> StencilSnapshots { get; set; } = [];

    #endregion
}