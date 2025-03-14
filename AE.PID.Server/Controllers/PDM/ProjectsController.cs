using AE.PID.Server.Core;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AE.PID.Server.Controllers;

[ApiController]
[Route("api/v{apiVersion:apiVersion}/[controller]")]
[ApiVersion(3)]
public class ProjectsController(
    IProjectService projectService)
    : ControllerBase
{
    /// <summary>
    ///     获取项目信息。
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="query"></param>
    /// <param name="pageNo"></param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetProjects([FromHeader(Name = "User-ID")] string userId,
        [FromQuery] string query = "",
        [FromQuery] int pageNo = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await projectService.GetPagedProjects(query, pageNo, pageSize, userId);
            return Ok(result);
        }
        catch (HttpRequestException e)
        {
            return BadRequest(e);
        }
    }

    /// <summary>
    ///     根据Id获取项目信息。
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProjectsById([FromHeader(Name = "User-ID")] string userId,
        int id)
    {
        try
        {
            var project = await projectService.GetProjectById(id, userId);

            if (project == null) return NotFound();
            return Ok(project);
        }
        catch (HttpRequestException e)
        {
            return BadRequest(e);
        }
    }
}