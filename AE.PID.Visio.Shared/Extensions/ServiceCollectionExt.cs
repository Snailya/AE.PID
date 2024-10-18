using AE.PID.Visio.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AE.PID.Visio.Shared.Extensions;

public static class ServiceCollectionExt
{
    public static void AddApi<TApi>(this IServiceCollection services)
    {
        services.AddTransient<IApiFactory<TApi>>(provider =>
            new ApiFactory<TApi>(provider.GetRequiredService<IConfigurationService>()));
    }
}