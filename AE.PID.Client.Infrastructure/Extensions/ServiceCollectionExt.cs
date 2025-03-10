using AE.PID.Client.Core;
using Microsoft.Extensions.DependencyInjection;

namespace AE.PID.Client.Infrastructure;

public static class ServiceCollectionExt
{
    public static void AddApi<TApi>(this IServiceCollection services)
    {
        services.AddTransient<IApiFactory<TApi>>(provider =>
            new ApiFactory<TApi>(provider.GetRequiredService<IConfigurationService>()));
    }
}