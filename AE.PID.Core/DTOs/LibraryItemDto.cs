﻿namespace AE.PID.Core;

public class LibraryItemDto
{
    /// <summary>
    ///     The name of the item which displayed in visio.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     The Unique id of the item, if the unique id is not equal to the unique id of the item in document stencil,
    ///     indicates that the item used in document stencil is not the same as the library, which means a update is needed.
    /// </summary>
    public string UniqueId { get; set; } = string.Empty;

    /// <summary>
    ///     The id used for deciding which item in a library is of the same origin with the item in document stencil.
    /// </summary>
    public string BaseId { get; set; } = string.Empty;
}