namespace AE.PID.Client.Core;

public class Resolved<T>(T value, ResolveType from)
{
    public ResolveType From { get; private set; } = from;
    public T Value { get; private set; } = value;
}

public enum ResolveType
{
    Network, // if the value is resolved from the web api
    Cache // if the value is resolved from the local cache
}