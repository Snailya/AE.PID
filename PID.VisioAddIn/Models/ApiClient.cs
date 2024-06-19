using System;
using System.Net.Http;
using System.Reactive.Disposables;
using AE.PID.Services;
using ReactiveUI;

namespace AE.PID.Models;

public class ApiClient : HttpClient, IDisposable
{
    private const string UserIdHeaderName = "User-Id";
    private readonly CompositeDisposable _cleanUp = new();

    public ApiClient(ConfigurationService configuration)
    {
        configuration.WhenAnyValue(x => x.Server)
            .Subscribe(server => { BaseAddress = new Uri(server); })
            .DisposeWith(_cleanUp);

        configuration.WhenAnyValue(x => x.UserId)
            .Subscribe(id =>
            {
                if (DefaultRequestHeaders.Contains(UserIdHeaderName)) DefaultRequestHeaders.Remove(UserIdHeaderName);

                DefaultRequestHeaders.Add(UserIdHeaderName, id);
            })
            .DisposeWith(_cleanUp);
    }
}