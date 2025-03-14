using System.Threading.Tasks;
using AE.PID.Core;
using Refit;

namespace AE.PID.Client.Infrastructure;

public interface IAppApi
{
    [Get("/api/v3/app")]
    Task<AppVersionDto> GetCurrentApp();
}