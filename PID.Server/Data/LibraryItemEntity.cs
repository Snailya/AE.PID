﻿namespace AE.PID.Server.Data;

public class LibraryItemEntity
{
    public int VersionId { get; set; }
    public LibraryVersionEntity Version { get; set; }

    public int Id { get; set; }

    /// <summary>
    ///     The name of the item which displayed in visio.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     The Unique id of the item, if the unique id is not equal to the unique id of the item in document stencil,
    ///     indicates that the item used in document stencil is not the same as the library, which means a update is needed.
    /// </summary>
    public string UniqueId { get; set; }

    /// <summary>
    ///     The id used for deciding which item in library is of the same origin with the item in document stencil.
    /// </summary>
    public string BaseId { get; set; }

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
    public string MasterElement { get; set; }

    /// <summary>
    ///     The string of XDocument that stands for /visio/masters/master{i}.xml
    /// </summary>
    public string MasterDocument { get; set; }
}