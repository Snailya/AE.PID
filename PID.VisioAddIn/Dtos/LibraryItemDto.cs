namespace AE.PID.Dtos;

public class LibraryItemDto
{
    /// <summary>
    ///     The name of the item which displayed in visio.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     The Unique id of the master's UniqueID property.
    ///     The unique id is used as the identifier for the master.
    ///     Whenever the master is edited in the Visio, the UniqueID property changes. 
    /// </summary>
    public string UniqueId { get; set; } = string.Empty;

    /// <summary>
    ///    The BaseID property of a master.
    ///    When the master is edited, the BaseID property remains unchanged. So it can be used as an unique identifier in the library.
    /// </summary>
    public string BaseId { get; set; } = string.Empty;
}