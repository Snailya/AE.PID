using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using AE.PID.Core.DTOs;
using AE.PID.Server.Core;
using AE.PID.Server.PDMS.Extensions;
using DynamicData;

namespace AE.PID.Server.PDMS;

public partial class FunctionService : IFunctionService
{
    private readonly HttpClient _bipClient;
    private readonly HttpClient _client;

    private readonly SourceCache<FunctionDto, string> _standardCaches = new(x => x.Code);

    public FunctionService(IHttpClientFactory httpClientFactory)
    {
        _bipClient = httpClientFactory.CreateClient("PDMS");
        _client = httpClientFactory.CreateClient("PDMS");

        _standardCaches.ExpireAfter(_ => TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10)).Subscribe();
    }

    public async Task<IEnumerable<FunctionDto>> GetProjectFunctionZonesAsync(string userId, string projectId)
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
        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();
        if (!string.IsNullOrEmpty(responseData?.Result))
        {
            var zones = JsonSerializer
                .Deserialize<IEnumerable<SelectProjectProcessSectionResponseItemDto>>(responseData.Result)
                ?.Select(x => x.FromPDMS());
            return zones!;
        }

        // todo: 检查返回可能的结果
        throw new HttpRequestException();
    }

    public async Task<IEnumerable<FunctionDto>> GetStandardFunctionGroupsAsync(string userId)
    {
        if (_standardCaches.Count != 0) return _standardCaches.Items;

        var data = PDMSApiResolver.BuildFormUrlEncodedContent(new SelectFunctionGroupRequestDto
        {
            OperationInfo = new OperationInfoDto { Operator = userId },
            MainTable = new FunctionGroupDto(),
            PageInfo = new PageInfoDto(1, 999)
        });

        var response = await _client.PostAsync("getModeDataPageList/selectFunctionGroup", data);
        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();
        if (string.IsNullOrEmpty(responseData?.Result)) return null;

        var groups = JsonSerializer
            .Deserialize<IEnumerable<SelectFunctionGroupResponseItemDto>>(responseData.Result)
            ?.Select(x => x.FromPDMS()).ToList();

        if (groups != null && groups.Count != 0)
            _standardCaches.AddOrUpdate(groups);

        return groups;
    }

    public async Task<string> SynFunctions(string userId,
        string uuid,
        string projectId,
        string functionId,
        List<FunctionDto> subFunctions)
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
                        IsEnabled = x.IsEnabled,
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
        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();
        if (string.IsNullOrEmpty(responseData?.Result)) return null;

        return responseData.Result;
    }

    public async Task<IEnumerable<FunctionDto>> GetProjectFunctionGroupsAsync(string userId, string projectId,
        string functionId)
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
        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();
        if (string.IsNullOrEmpty(responseData?.Result)) return null;

        var functionGroups = JsonSerializer
            .Deserialize<IEnumerable<SelectProjectFunctionGroupResponseItemDto>>(responseData.Result)
            ?.Select(x => x.FromPDMS());

        return functionGroups;
    }

    [GeneratedRegex(@"([A-Za-z]+)(\d+)")]
    private static partial Regex MyRegex();
}