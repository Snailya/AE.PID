namespace AE.PID.Dtos;

public class DetailedLibraryItemDto : LibraryItemDto
{
    /// <summary>
    ///     The line style that applies on the item.
    /// </summary>
    public string LineStyleName { get; set; } = string.Empty;

    /// <summary>
    ///     The fill style that applies on the item.
    /// </summary>
    public string FillStyleName { get; set; } = string.Empty;

    /// <summary>
    ///     The text style that applies on the item.
    /// </summary>
    public string TextStyleName { get; set; } = string.Empty;

    /// <summary>
    ///     The string of XElement that stands for Master Element in /visio/masters/maters.xml
    /// </summary>
    public string MasterElement { get; set; } = string.Empty;

    /// <summary>
    ///     The string of XDocument that stands for /visio/masters/master{i}.xml
    /// </summary>
    public string MasterDocument { get; set; } = string.Empty;
}