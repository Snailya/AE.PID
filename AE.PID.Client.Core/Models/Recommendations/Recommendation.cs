namespace AE.PID.Client.Core;

public class Recommendation<T>
{
    /// <summary>
    ///     The id of the recommendation collection that this item that belongs to.
    /// </summary>
    public int CollectionId { get; set; }

    /// <summary>
    ///     The item id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     The display order of this item.
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    ///     The value of the item.
    /// </summary>
    public T Data { get; set; } = default!;

    /// <summary>
    ///     The collection of algorithm names that this item is generated from.
    /// </summary>
    public string Algorithm { get; set; } = string.Empty;
}