using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AE.PID.Server.Data;

public class LibraryVersionItemXML : EntityBase
{
    /// <summary>
    ///     The line style that applies on the item.
    /// </summary>
    [Required]
    public string LineStyleName { get; set; }

    /// <summary>
    ///     The fill style that applies on the item.
    /// </summary>
    [Required]
    public string FillStyleName { get; set; }

    /// <summary>
    ///     The text style that applies on the item.
    /// </summary>
    [Required]
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

    public int LibraryVersionItemId { get; set; }
    [ForeignKey("LibraryVersionItemId")] public LibraryItem LibraryItem { get; set; }

    #endregion
}