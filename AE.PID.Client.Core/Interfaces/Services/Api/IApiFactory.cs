using System.Net.Http;

namespace AE.PID.Client.Core;

public interface IApiFactory<T>
{
    HttpClient HttpClient { get; }
    T Api { get; }
}