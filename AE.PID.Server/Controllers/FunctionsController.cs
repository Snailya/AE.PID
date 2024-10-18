using System.Text.Json;
using System.Text.RegularExpressions;
using AE.PID.Core.DTOs;
using AE.PID.Server.DTOs;
using AE.PID.Server.DTOs.PDMS;
using AE.PID.Server.Services;
using Asp.Versioning;
using DynamicData;
using Microsoft.AspNetCore.Mvc;

namespace AE.PID.Server.Controllers;

[ApiController]
[ApiVersion(3)]
[Route("api/v{apiVersion:apiVersion}/[controller]")]
public partial class FunctionsController : ControllerBase
{
    private readonly HttpClient _bipClient;
    private readonly HttpClient _client;

    private readonly SourceCache<FunctionDto, string> _standardCaches = new(x => x.Code);

    public FunctionsController(ILogger<FunctionsController> logger,
        IHttpClientFactory httpClientFactory)
    {
        _bipClient = httpClientFactory.CreateClient("PDMS");
        _client = httpClientFactory.CreateClient("PDMS");

        _standardCaches.ExpireAfter(_ => TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10)).Subscribe();
    }

    private async Task<IActionResult> GetProjectFunctionZonesAsync(string userId, string projectId)
    {
        var data = PDMSApiResolver.BuildFormUrlEncodedContent(new SelectProjectProcessSectionRequestDto
        {
            OperationInfo = new OperationInfoDto { Operator = userId },
            MainTable = new ProjectProcessSectionDto
            {
                ProjectId = projectId
            },
            PageInfo = new PageInfoDto(1, 100)
        });

        var response = await _client.PostAsync("getModeDataPageList/selectProjectProcessSection", data);
        if (!response.IsSuccessStatusCode) return BadRequest("Failed to send form data to the API");

        var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();
        if (string.IsNullOrEmpty(responseData?.Result)) return NoContent();

        var zones = JsonSerializer
            .Deserialize<IEnumerable<SelectProjectProcessSectionResponseItemDto>>(responseData.Result)
            ?.Select(x => x.FromPDMS());

        return Ok(zones);
    }

    private async Task<IActionResult> GetStandardFunctionGroupsAsync(string userId)
    {
        if (_standardCaches.Count != 0) return Ok(_standardCaches.Items);

        var data = PDMSApiResolver.BuildFormUrlEncodedContent(new SelectFunctionGroupRequestDto
        {
            OperationInfo = new OperationInfoDto { Operator = userId },
            MainTable = new FunctionGroupDto(),
            PageInfo = new PageInfoDto(1, 999)
        });

        var response = await _client.PostAsync("getModeDataPageList/selectFunctionGroup", data);
        if (!response.IsSuccessStatusCode) return BadRequest("Failed to send form data to the API");

        var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();
        if (string.IsNullOrEmpty(responseData?.Result)) return NoContent();

        var groups = JsonSerializer
            .Deserialize<IEnumerable<SelectFunctionGroupResponseItemDto>>(responseData.Result)
            ?.Select(x => x.FromPDMS()).ToList();

        if (groups != null && groups.Count != 0)
            _standardCaches.AddOrUpdate(groups);

        return Ok(groups);
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
        if (_standardCaches.Items.Count == 0) await GetStandardFunctionGroupsAsync(userId);

        var itemDtos = subFunctions.Select(x =>
            {
                var match = MyRegex().Match(x.Code);
                if (match.Success)
                {
                    var template = _standardCaches.Lookup(match.Groups[1].Value);

                    return new SyncProjectFunctionGroupItemDto
                    {
                        Id = x.Id.ToString(),
                        Number = match.Groups[2].Value,
                        IsEnabled = false,
                        TemplatedId = template.HasValue ? template.Value.Id.ToString() : string.Empty
                    };
                }

                return null;
            })
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        var data = new SyncProjectFunctionGroupsRequestDto
        {
            Header = PDMSApiResolver.CreateBipHeader(userId, uuid, BipActions.SyncProjectFunctionGroups),
            Body = new SyncProjectFunctionGroupsDto
            {
                ProjectId = projectId,
                ZoneId = functionId,
                UserId = userId,
                DeviceId = uuid,
                Items = itemDtos
            }
        };

        var response = await _bipClient.PostAsJsonAsync(data.Header.BipCode, data);
        if (!response.IsSuccessStatusCode) return BadRequest("Failed to send json data to the API");

        var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();
        if (string.IsNullOrEmpty(responseData?.Result)) return NoContent();

        return Ok(responseData);
    }

    private async Task<IActionResult> GetProjectFunctionGroupsAsync(string userId, string projectId, string functionId)
    {
        var data = PDMSApiResolver.BuildFormUrlEncodedContent(new SelectProjectFunctionGroupRequestDto
        {
            OperationInfo = new OperationInfoDto { Operator = userId },
            MainTable = new ProjectFunctionGroupDto
            {
                ProjectId = projectId,
                ProjectProcessSection = functionId
            },
            PageInfo = new PageInfoDto(1, 99)
        });

        var response = await _client.PostAsync("getModeDataPageList/selectProjectFunctionGroup", data);
        if (!response.IsSuccessStatusCode) return BadRequest("Failed to send form data to the API");

        var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();
        if (string.IsNullOrEmpty(responseData?.Result)) return NoContent();

        var functionGroups = JsonSerializer
            .Deserialize<IEnumerable<SelectProjectFunctionGroupResponseItemDto>>(responseData.Result)
            ?.Select(x => x.FromPDMS());

        return Ok(functionGroups);
    }

    [GeneratedRegex(@"([A-Za-z]+)(\d+)")]
    private static partial Regex MyRegex();

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