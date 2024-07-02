using System;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using ReactiveUI;

namespace AE.PID.Services;

public class ApiClient : IDisposable
{
    private const string UserIdHeaderName = "User-Id";
    private readonly CompositeDisposable _cleanUp = new();
    private HttpClient _client = new();

    public ApiClient(ConfigurationService configuration)
    {
        configuration.WhenAnyValue(x => x.Server)
            .Subscribe(server =>
            {
                _client = new HttpClient { BaseAddress = new Uri(server) };
                SetUserId(configuration.UserId);
            })
            .DisposeWith(_cleanUp);

        configuration.WhenAnyValue(x => x.UserId)
            .Subscribe(SetUserId)
            .DisposeWith(_cleanUp);
    }

    public void Dispose()
    {
        _cleanUp.Dispose();
        _client.Dispose();
    }

    private void SetUserId(string id)
    {
        if (_client.DefaultRequestHeaders.Contains(UserIdHeaderName))
            _client.DefaultRequestHeaders.Remove(UserIdHeaderName);

        _client.DefaultRequestHeaders.Add(UserIdHeaderName, id);
    }

    public Task<HttpResponseMessage> GetAsync(string requestUri)
    {
        return _client.GetAsync(requestUri);
    }

    public Task<string> GetStringAsync(string requestUri)
    {
        return _client.GetStringAsync(requestUri);
    }
}