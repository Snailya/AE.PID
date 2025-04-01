using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AE.PID.Core;

public class Paged<T> : IPageResponse
{
    public IEnumerable<T> Items { get; set; } = [];

    /// <summary>
    ///     Gets the current page.
    /// </summary>
    [JsonPropertyName("pageNo")]
    public int Page { get; set; }

    /// <summary>
    ///     Gets total number of pages.
    /// </summary>
    [JsonPropertyName("pagesCount")]
    public int Pages { get; set; }

    /// <summary>
    ///     Gets the total number of records in the underlying cache.
    /// </summary>
    [JsonPropertyName("itemsCount")]
    public int TotalSize { get; set; }

    /// <summary>
    ///     Gets the size of the page.
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }
}