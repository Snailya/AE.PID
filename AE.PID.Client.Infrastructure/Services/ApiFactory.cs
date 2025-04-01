using System;
using System.Linq;
using System.Net.Http;
using AE.PID.Client.Core;
using Refit;
using Splat;

namespace AE.PID.Client.Infrastructure;

public class ApiFactory<T> : DisposableBase, IApiFactory<T>
{
    public ApiFactory(IConfigurationService configurationService)
    {
        var subscription = configurationService.Configuration.Subscribe(x =>
        {
            // create a new http client if not exist or the url changed
            if (HttpClient == null || HttpClient.BaseAddress.AbsolutePath != x.Server)
            {
                this.Log().Debug($"Creating a http client for {typeof(T).Name}...");

                HttpClient = new HttpClient
                {
                    BaseAddress = new Uri(x.Server)
                };

                // append UUID as header
                HttpClient.DefaultRequestHeaders.Add("UUID", configurationService.RuntimeConfiguration.UUID);

                Api = RestService.For<T>(HttpClient);

                this.Log().Debug(
                    $"ApiBase for {typeof(T).Name} created. Headers: [UUID: {configurationService.RuntimeConfiguration.UUID}]");
            }

            if (HttpClient.DefaultRequestHeaders.TryGetValues("User-ID", out var values) &&
                values.Any(i => i == x.UserId)) return;

            this.Log().Debug($"The User-ID Header for {typeof(T).Name} needs update.");

            // update the header
            HttpClient.DefaultRequestHeaders.Remove("User-ID");
            HttpClient.DefaultRequestHeaders.Add("User-ID", x.UserId);

            this.Log().Debug($"The User-ID Header for {typeof(T).Name} updated. The current User-ID is {x.UserId}");
        });

        CleanUp.Add(subscription);
    }

    public HttpClient HttpClient { get; private set; } = null!;
    public T Api { get; private set; }
}