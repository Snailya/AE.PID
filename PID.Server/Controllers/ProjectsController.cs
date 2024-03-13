using System.Text.Json;
using AE.PID.Core.DTOs;
using AE.PID.Server.DTOs;
using AE.PID.Server.DTOs.PDMS;
using AE.PID.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace AE.PID.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class ProjectsController(
    ILogger<ProjectsController> logger,
    LinkGenerator linkGenerator,
    IHttpClientFactory httpClientFactory)
    : ControllerBase
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("PDMS");

    [HttpGet]
    public async Task<IActionResult> GetProjects([FromQuery] int pageNo = 1,
        [FromQuery] int pageSize = 10)
    {
        var count = await GetProjectsCount("");

        var data = ApiHelper.BuildFormUrlEncodedContent(new SelectNewProjectInfoRequestDto()
        {
            OperationInfo = new OperationInfoDto { Operator = "6470" },
            MainTable = new ProjectInfoDto
            {
                StatusId = "1"
            },
            PageInfo = new PageInfoDto(1, 10)
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
            PageNo = pageNo,
            PageSize = pageSize,
            PagesCount = (int)Math.Ceiling((double)count / pageSize),
            ItemsCount = count,
            Items = projects
        });
    }

    private async Task<int> GetProjectsCount(string id)
    {
        var data = new CountNewProjectRequestDto
        {
            OperationInfo = new OperationInfoDto { Operator = "6470" },
            MainTable = new
            {
                Id = id
            }
        };
        var content = ApiHelper.BuildFormUrlEncodedContent(data);

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