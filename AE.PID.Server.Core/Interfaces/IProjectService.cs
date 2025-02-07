using AE.PID.Core.DTOs;
using AE.PID.Visio.Core.DTOs;

namespace AE.PID.Server.Core;

public interface IProjectService
{
    Task<Paged<ProjectDto>> GetPagedProjects(string query, int pageNumber, int pageSize, string userId);
    Task<ProjectDto?> GetProjectById(int id, string userId);
}