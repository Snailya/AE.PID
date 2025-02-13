namespace AE.PID.Client.Core;

public class ResolveResult<T>(T value, DataSource resolveFrom)
{
    /// <summary>
    ///     If a data is failed to solve, create an instance with only id value setted.
    /// </summary>
    public T Value { get; } = value;

    /// <summary>
    ///     The source that the value come from
    /// </summary>
    public DataSource ResolveFrom { get; } = resolveFrom;

    /// <summary>
    ///     The information when resolving the data
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

public enum DataSource
{
    Api,
    LocalCache,
    Unknown
}