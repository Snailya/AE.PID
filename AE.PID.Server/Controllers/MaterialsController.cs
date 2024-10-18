using System.Text.Json;
using AE.PID.Core.DTOs;
using AE.PID.Server.DTOs;
using AE.PID.Server.DTOs.PDMS;
using AE.PID.Server.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AE.PID.Server.Controllers;

[ApiController]
[Route("api/v{apiVersion:apiVersion}/[controller]")]
[ApiVersion(3)]
public class MaterialsController(IHttpClientFactory httpClientFactory)
    : ControllerBase
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("PDMS");

    /// <summary>
    ///     根据编码获取物料。
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    [HttpGet("{code}")]
    public async Task<IActionResult> GetMaterialsByCode([FromHeader(Name = "User-ID")] string userId,
        [FromRoute] string code)
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
        if (!response.IsSuccessStatusCode) return BadRequest("Failed to send form data to the API");
        var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();
        if (string.IsNullOrEmpty(responseData?.Result)) return Ok(null);

        var material =
            JsonSerializer.Deserialize<IEnumerable<SelectDesignMaterialResponseItemDto>>(responseData.Result)?
                .Select(x => x.FromPDMS()).First();
        return Ok(material);
    }

    /// <summary>
    ///     获取物料。
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="category"></param>
    /// <param name="s"></param>
    /// <param name="pageNo"></param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetMaterials([FromHeader(Name = "User-ID")] string userId,
        [FromQuery] string? category = null, [FromQuery] string? s = null,
        [FromQuery] int pageNo = 1,
        [FromQuery] int pageSize = 10)
    {
        var count = await GetMaterialsCount(category ?? string.Empty);

        var data = PDMSApiResolver.BuildFormUrlEncodedContent(new SelectDesignMaterialRequestDto
        {
            OperationInfo = new OperationInfoDto { Operator = userId },
            MainTable = new DesignMaterialDto
            {
                MaterialCategory = category ?? string.Empty,
                MaterialName = s ?? string.Empty
            },
            PageInfo = new PageInfoDto(pageNo, pageSize)
        });

        var response = await _client.PostAsync("getModeDataPageList/selectDesignMaterial", data);

        if (!response.IsSuccessStatusCode) return BadRequest("Failed to send form data to the API");

        var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();
        if (string.IsNullOrEmpty(responseData?.Result)) return NoContent();

        var materials =
            JsonSerializer.Deserialize<IEnumerable<SelectDesignMaterialResponseItemDto>>(responseData.Result)?
                .Select(x => x.FromPDMS());
        return Ok(new Paged<MaterialDto>
        {
            Page = pageNo,
            PageSize = pageSize,
            Pages = (int)Math.Ceiling((double)count / pageSize),
            TotalSize = count,
            Items = materials
        });
    }

    private Task<int> GetMaterialsCount(string category)
    {
        return GetMaterialsCount("", "", "", category, "", "", "");
    }

    private async Task<int> GetMaterialsCount(string name, string code, string model, string category, string brand,
        string specifications, string manufacturer)
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
}