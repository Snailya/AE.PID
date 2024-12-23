﻿using System.Threading.Tasks;
using AE.PID.Core.DTOs;
using AE.PID.Visio.Core.DTOs;
using Refit;

namespace AE.PID.Visio.Shared;

public interface IProjectApi
{
    [Get("/api/v3/projects")]
    Task<Paged<ProjectDto>> GetProjectsAsync([Query] string query, [Query] int pageNo, [Query] int pageSize);

    [Get("/api/v3/projects/{id}")]
    Task<ProjectDto> GetProjectByIdAsync(int id);
}