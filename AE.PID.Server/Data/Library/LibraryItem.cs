using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AE.PID.Server.Data;

public class LibraryItem : EntityBase
{
    /// <summary>
    ///     The name of the item which displayed in visio.
    /// </summary>
    [Required]
    public string Name { get; set; }

    /// <summary>
    ///     The Unique id of the item, if the unique id is not equal to the unique id of the item in document stencil,
    ///     indicates that the item used in document stencil is not the same as the library, which means a update is needed.
    /// </summary>
    [MaxLength(36)]
    [Required]
    public string UniqueId { get; set; } = string.Empty;

    /// <summary>
    ///     The id used for deciding which item in library is of the same origin with the item in document stencil.
    /// </summary>
    [MaxLength(36)]
    [Required]
    public string BaseId { get; set; } = string.Empty;


    #region -- Navigation Properties --

    public int LibraryVersionEntityId { get; set; }

    [ForeignKey("LibraryVersionEntityId")] public LibraryVersion LibraryVersion { get; set; }

    public int LibraryVersionItemXmlId { get; set; }
    public LibraryVersionItemXML LibraryVersionItemXML { get; set; }

    #endregion
}