using System.Threading.Tasks;
using AE.PID.Core;
using Refit;

namespace AE.PID.Client.Infrastructure;

public interface IProjectApi
{
    [Get("/api/v3/projects")]
    Task<Paged<ProjectDto>> GetProjectsAsync([Query] string query, [Query] int pageNo, [Query] int pageSize);

    [Get("/api/v3/projects/{id}")]
    Task<ProjectDto> GetProjectByIdAsync(int id);
}