using AE.PID.Core.DTOs;

namespace AE.PID.Server.Core;

public interface IFunctionService
{
    Task<IEnumerable<FunctionDto>> GetProjectFunctionZonesAsync(string userId, string projectId);
    Task<IEnumerable<FunctionDto>> GetStandardFunctionGroupsAsync(string userId);

    Task<string> SynFunctions(string userId,
        string uuid,
        string projectId,
        string functionId,
        List<FunctionDto> subFunctions);

    Task<IEnumerable<FunctionDto>> GetProjectFunctionGroupsAsync(string userId, string projectId, string functionId);
}