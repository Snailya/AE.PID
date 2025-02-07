using AE.PID.Core.DTOs;
using AE.PID.Server.Core;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AE.PID.Server.Controllers;

[ApiController]
[ApiVersion(3)]
[Route("api/v{apiVersion:apiVersion}/[controller]")]
public class FunctionsController(IFunctionService functionService) : ControllerBase
{
    private async Task<IActionResult> GetProjectFunctionZonesAsync(string userId, string projectId)
    {
        try
        {
            var zones = await functionService.GetProjectFunctionZonesAsync(userId, projectId);
            return Ok(zones);
        }
        catch (HttpRequestException e)
        {
            return BadRequest(e);
        }
    }

    private async Task<IActionResult> GetStandardFunctionGroupsAsync(string userId)
    {
        try
        {
            var groups = await functionService.GetStandardFunctionGroupsAsync(userId);
            return Ok(groups);
        }
        catch (HttpRequestException e)
        {
            return BadRequest(e);
        }
    }

    /// <summary>
    ///     获取功能位信息。
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="projectId"></param>
    /// <param name="functionId"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetFunctions([FromHeader(Name = "User-ID")] string userId,
        [FromQuery] string? projectId = null,
        [FromQuery] string? functionId = null)
    {
        if (projectId == null && functionId == null) return await GetStandardFunctionGroupsAsync(userId);

        if (projectId != null && functionId == null)
            return await GetProjectFunctionZonesAsync(userId, projectId);
        if (projectId != null && functionId != null)
            return await GetProjectFunctionGroupsAsync(userId, projectId, functionId);

        return BadRequest();
    }

    /// <summary>
    ///     向PMDS同步功能组
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="uuid"></param>
    /// <param name="projectId">待同步的项目Id</param>
    /// <param name="functionId">待同步的工艺区域Id</param>
    /// <param name="subFunctions">待同步的功能组信息。</param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> SynFunctions([FromHeader(Name = "User-ID")] string userId,
        [FromHeader(Name = "UUID")] string uuid,
        [FromQuery] string projectId,
        [FromQuery] string functionId,
        [FromBody] List<FunctionDto> subFunctions)
    {
        try
        {
            var groups = await functionService.SynFunctions(userId, uuid, projectId, functionId, subFunctions);
            return Ok(groups);
        }
        catch (HttpRequestException e)
        {
            return BadRequest(e);
        }
    }

    private async Task<IActionResult> GetProjectFunctionGroupsAsync(string userId, string projectId, string functionId)
    {
        try
        {
            var groups = await functionService.GetProjectFunctionGroupsAsync(userId, projectId, functionId);
            return Ok(groups);
        }
        catch (HttpRequestException e)
        {
            return BadRequest(e);
        }
    }


    // private SyncProjectFunctionGroupItemDto ToSyncProjectFunctionGroup(FunctionDto dto)
    // {
    //     var prefix = 
    //     _standardCaches.Lookup(dto.Code.)
    //     
    //     return new SyncProjectFunctionGroupItemDto
    //     {
    //         Id = null,
    //         Number = null,
    //         IsEnabled = false,
    //         TemplatedId = null
    //     };
    // }
}