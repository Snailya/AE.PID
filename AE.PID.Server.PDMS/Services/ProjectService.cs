using System.Net.Http.Json;
using System.Text.Json;
using AE.PID.Core;
using AE.PID.Server.Core;
using AE.PID.Server.PDMS.Extensions;

namespace AE.PID.Server.PDMS;

public class ProjectService(IHttpClientFactory httpClientFactory) : IProjectService
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("PDMS");

    public async Task<Paged<ProjectDto>?> GetPagedProjects(string query, int pageNumber, int pageSize, string userId)
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
            PageInfo = new PageInfoDto(pageNumber, pageSize)
        });

        var response = await _client.PostAsync("getModeDataPageList/selectNewProjectInfo", data);
        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();
        if (string.IsNullOrEmpty(responseData?.Result))
            throw new HttpRequestException("API response content is empty.");

        var projects = JsonSerializer
            .Deserialize<IEnumerable<SelectNewProjectInfoResponseItemDto>>(responseData.Result)
            ?.Select(x => x.FromPDMS())
            .ToArray();

        if (projects == null || !projects.Any()) return null;

        return new Paged<ProjectDto>
        {
            Page = pageNumber,
            PageSize = pageSize,
            Pages = (int)Math.Ceiling((double)count / pageSize),
            TotalSize = count,
            Items = projects
        };
    }

    public async Task<ProjectDto?> GetProjectById(int id, string userId)
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
        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();
        // todo: 20250127 需要检查PDMS输入不存在的id的时候返回的是null，还是报错，如果返回的是null，则此处不应该认为是异常。
        if (string.IsNullOrEmpty(responseData?.Result))
            throw new HttpRequestException("API response content is empty.");

        var project = JsonSerializer
            .Deserialize<IEnumerable<SelectNewProjectInfoResponseItemDto>>(responseData.Result)
            ?.Select(x => x.FromPDMS()).SingleOrDefault(x => x.Id == id);

        return project;
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
        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();

        // todo: 此处也是需要确定会不会返回错误的结果，如果确定不会，则
        if (string.IsNullOrEmpty(responseData?.Result))
            throw new HttpRequestException("API response content is empty.");

        var pageCountDto =
            JsonSerializer.Deserialize<PageCountDto>(responseData.Result);

        return pageCountDto!.PageCount;
    }
}