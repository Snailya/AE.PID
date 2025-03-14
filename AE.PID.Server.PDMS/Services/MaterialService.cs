using System.Net.Http.Json;
using System.Text.Json;
using AE.PID.Core;
using AE.PID.Server.Core;
using AE.PID.Server.PDMS.Extensions;

namespace AE.PID.Server.PDMS;

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
        response.EnsureSuccessStatusCode();

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
        // todo: 此处同其他位置
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

        throw new HttpRequestException($"Failed to get materials count. Keywords:{data.GetQuery()}");
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
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();
        // todo: 此处同其他位置
        if (string.IsNullOrEmpty(responseData?.Result)) return null;

        var material =
            JsonSerializer.Deserialize<IEnumerable<SelectDesignMaterialResponseItemDto>>(responseData.Result)?
                .Select(x => x.FromPDMS()).First();
        return material;
    }

    public async Task<IEnumerable<MaterialCategoryDto>> GetCategories(string userId, string? name = null)
    {
        var categories = (await GetCategoryItems(userId, name))?.Select(x => x.FromPDMS());
        return categories;
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

        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();
        // todo: 此处同其他位置
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


    private async Task<IEnumerable<SelectDesignMaterialCategoryResponseItemDto>> GetCategoryItems(string userId,
        string? name = null)
    {
        var data = PDMSApiResolver.BuildFormUrlEncodedContent(new SelectDesignMaterialCategoryRequestDto
        {
            OperationInfo = new OperationInfoDto { Operator = userId },
            MainTable = new DesignMaterialCategoryDto { CategoryName = name ?? "" },
            PageInfo = new PageInfoDto(1, 10000)
        });

        var response = await _client.PostAsync("getModeDataPageList/selectDesignMaterialCategory", data);

        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();
        // todo: 此处同其他位置
        if (string.IsNullOrEmpty(responseData?.Result)) return [];

        var categories = JsonSerializer
            .Deserialize<IEnumerable<SelectDesignMaterialCategoryResponseItemDto>>(responseData.Result);

        // Members that return a sequence should never return null. Return an empty sequence instead
        return categories ?? [];
    }
}