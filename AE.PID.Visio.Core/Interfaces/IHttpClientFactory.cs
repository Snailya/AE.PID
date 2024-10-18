namespace AE.PID.Visio.Core.Interfaces;

public interface IApiFactory<T>
{
    HttpClient HttpClient { get; }
    T Api { get; }
}