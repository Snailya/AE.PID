using System.Threading.Tasks;
using AE.PID.Core.DTOs;
using Refit;

namespace AE.PID.Visio.Shared;

public interface IAppApi
{
    [Get("/api/v3/app")]
    Task<AppVersionDto> GetCurrentApp();
}