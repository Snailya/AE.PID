using AE.PID.Server.Core;
using Microsoft.Extensions.DependencyInjection;

namespace AE.PID.Server.PDMS.Extensions;

public static class ServiceCollectionExt
{
    public static IServiceCollection AddPDMS(this IServiceCollection services)
    {
        // register a httpclient for PDMS
        services.AddHttpClient("PDMS",
            client => { client.BaseAddress = new Uri("http://172.18.168.57:8000/api/cube/restful/interface/"); });
        services.AddHttpClient("PDMSBip",
            client =>
            {
                client.BaseAddress =
                    new Uri("http://172.18.168.58:10000/api/weaver/bip/unified/business/interface/protocol/dispatch/");
            });

        services.AddTransient<IProjectService, ProjectService>();
        services.AddTransient<IFunctionService, FunctionService>();
        services.AddTransient<IMaterialService, MaterialService>();

        return services;
    }
}