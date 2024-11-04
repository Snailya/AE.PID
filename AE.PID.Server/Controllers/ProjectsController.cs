using System.Text.Json;
using AE.PID.Core.DTOs;
using AE.PID.Server.DTOs;
using AE.PID.Server.DTOs.PDMS;
using AE.PID.Server.Extensions;
using AE.PID.Server.Services;
using AE.PID.Visio.Core.DTOs;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AE.PID.Server.Controllers;

[ApiController]
[Route("api/v{apiVersion:apiVersion}/[controller]")]
[ApiVersion(3)]
public class ProjectsController(
    IHttpClientFactory httpClientFactory)
    : ControllerBase
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("PDMS");


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
        var count = await GetProjectsCount(userId, query);

        var data = PDMSApiResolver.BuildFormUrlEncodedContent(new SelectNewProjectInfoRequestDto
        {
            OperationInfo = new OperationInfoDto { Operator = userId },
            MainTable = new ProjectInfoDto
            {
                StatusId = "1",
                ProjectName = query
            },
            PageInfo = new PageInfoDto(pageNo, pageSize)
        });

        var response = await _client.PostAsync("getModeDataPageList/selectNewProjectInfo", data);
        if (!response.IsSuccessStatusCode) return BadRequest("Failed to send form data to the API");

        var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();
        if (string.IsNullOrEmpty(responseData?.Result)) return NoContent();

        var projects = JsonSerializer
            .Deserialize<IEnumerable<SelectNewProjectInfoResponseItemDto>>(responseData.Result)
            ?.Select(x => x.FromPDMS());

        return Ok(new Paged<ProjectDto>
        {
            Page = pageNo,
            PageSize = pageSize,
            Pages = (int)Math.Ceiling((double)count / pageSize),
            TotalSize = count,
            Items = projects
        });
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
        var data = PDMSApiResolver.BuildFormUrlEncodedContent(new SelectNewProjectInfoRequestDto
        {
            OperationInfo = new OperationInfoDto { Operator = userId },
            MainTable = new ProjectInfoDto
            {
                Id = id.ToString()
            },
            PageInfo = new PageInfoDto(1, 1)
        });

        var response = await _client.PostAsync("getModeDataPageList/selectNewProjectInfo", data);
        if (!response.IsSuccessStatusCode) return BadRequest("Failed to send form data to the API");

        var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();
        if (string.IsNullOrEmpty(responseData?.Result)) return NotFound();

        var project = JsonSerializer
            .Deserialize<IEnumerable<SelectNewProjectInfoResponseItemDto>>(responseData.Result)
            ?.Select(x => x.FromPDMS()).SingleOrDefault(x => x.Id == id);
        return Ok(project);
    }

    private async Task<int> GetProjectsCount(string userId, string? query = null)
    {
        var data = new CountNewProjectRequestDto
        {
            OperationInfo = new OperationInfoDto { Operator = userId },
            MainTable = new ProjectInfoDto
            {
                ProjectName = query ?? string.Empty
            }
        };
        var content = PDMSApiResolver.BuildFormUrlEncodedContent(data);

        var response = await _client.PostAsync("getModeDataPageCount/countNewProject", content);

        if (response.IsSuccessStatusCode)
        {
            var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();

            if (!string.IsNullOrEmpty(responseData?.Result))
            {
                var pageCountDto =
                    JsonSerializer.Deserialize<PageCountDto>(responseData.Result);

                if (pageCountDto != null)
                    return pageCountDto.PageCount;
            }
        }

        throw new BadHttpRequestException($"Failed to get projects count. Keywords:{data.MainTable}");
    }
}