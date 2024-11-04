using System.Text.Json;
using AE.PID.Core.DTOs;
using AE.PID.Server.DTOs.PDMS;
using AE.PID.Server.Extensions;
using AE.PID.Server.Services;

namespace AE.PID.Server.Interfaces;

public class MaterialService(IHttpClientFactory httpClientFactory) : IMaterialService
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("PDMS");

    public async Task<Paged<MaterialDto>?> GetMaterialsAsync(string userId,
        string? category = null, string? nameKeyword = null,
        int pageNo = 1,
        int pageSize = 10)
    {
        var count = await GetMaterialsCountAsync(category ?? string.Empty);

        var materials = await GetFlattenMaterialsAsync(userId, category, nameKeyword, pageNo, pageSize);

        if (materials == null) return null;

        return new Paged<MaterialDto>
        {
            Page = pageNo,
            PageSize = pageSize,
            Pages = (int)Math.Ceiling((double)count / pageSize),
            TotalSize = count,
            Items = materials
        };
    }

    public async Task<MaterialDto?> GetMaterialByCodeAsync(string userId, string code)
    {
        var data = PDMSApiResolver.BuildFormUrlEncodedContent(new SelectDesignMaterialRequestDto
        {
            OperationInfo = new OperationInfoDto { Operator = userId },
            MainTable = new DesignMaterialDto
            {
                MaterialCode = code
            },
            PageInfo = new PageInfoDto(1, 1)
        });
        var response = await _client.PostAsync("getModeDataPageList/selectDesignMaterial", data);
        if (!response.IsSuccessStatusCode) throw new BadHttpRequestException("Failed to send form data to the API");
        var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();
        if (string.IsNullOrEmpty(responseData?.Result)) return null;

        var material =
            JsonSerializer.Deserialize<IEnumerable<SelectDesignMaterialResponseItemDto>>(responseData.Result)?
                .Select(x => x.FromPDMS()).First();
        return material;
    }

    public async Task<int> GetMaterialsCountAsync(string name, string code, string model, string category, string brand,
        string specifications,
        string manufacturer)
    {
        var data = new SelectDesignMaterialRequestDto
        {
            OperationInfo = new OperationInfoDto { Operator = "6470" },
            MainTable = new DesignMaterialDto
            {
                MaterialName = name,
                MaterialCode = code,
                Model = model,
                MaterialCategory = category,
                Brand = brand,
                Specifications = specifications,
                Manufacturer = manufacturer
            }
        };
        var content = PDMSApiResolver.BuildFormUrlEncodedContent(data);

        var response = await _client.PostAsync("getModeDataPageCount/countDesignMaterial", content);

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

        throw new BadHttpRequestException($"Failed to get materials count. Keywords:{data.GetQuery()}");
    }

    public async Task<MaterialDto?> GetMaterialByIdAsync(string userId, int id)
    {
        var data = PDMSApiResolver.BuildFormUrlEncodedContent(new SelectDesignMaterialRequestDto
        {
            OperationInfo = new OperationInfoDto { Operator = userId },
            MainTable = new DesignMaterialDto
            {
                Id = id.ToString()
            },
            PageInfo = new PageInfoDto(1, 1)
        });
        var response = await _client.PostAsync("getModeDataPageList/selectDesignMaterial", data);
        if (!response.IsSuccessStatusCode) throw new BadHttpRequestException("Failed to send form data to the API");
        var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();
        if (string.IsNullOrEmpty(responseData?.Result)) return null;

        var material =
            JsonSerializer.Deserialize<IEnumerable<SelectDesignMaterialResponseItemDto>>(responseData.Result)?
                .Select(x => x.FromPDMS()).First();
        return material;
    }

    public async Task<IEnumerable<MaterialDto>?> GetFlattenMaterialsAsync(string userId,
        string? category = null, string? nameKeyword = null,
        int pageNo = 1,
        int pageSize = 10)
    {
        var data = PDMSApiResolver.BuildFormUrlEncodedContent(new SelectDesignMaterialRequestDto
        {
            OperationInfo = new OperationInfoDto { Operator = userId },
            MainTable = new DesignMaterialDto
            {
                MaterialCategory = category ?? string.Empty,
                MaterialName = nameKeyword ?? string.Empty
            },
            PageInfo = new PageInfoDto(pageNo, pageSize)
        });

        var response = await _client.PostAsync("getModeDataPageList/selectDesignMaterial", data);

        if (!response.IsSuccessStatusCode) throw new BadHttpRequestException("Failed to send form data to the API");

        var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();
        if (string.IsNullOrEmpty(responseData?.Result)) return null;

        var materials =
            JsonSerializer.Deserialize<IEnumerable<SelectDesignMaterialResponseItemDto>>(responseData.Result)?
                .Select(x => x.FromPDMS());

        return materials;
    }
    
    private Task<int> GetMaterialsCountAsync(string category)
    {
        return GetMaterialsCountAsync("", "", "", category, "", "", "");
    }
}