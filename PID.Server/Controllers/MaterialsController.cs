using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using AE.PID.Core.DTOs;
using AE.PID.Server.DTOs;
using AE.PID.Server.DTOs.PDMS;
using AE.PID.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace AE.PID.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class MaterialsController(
    ILogger<MaterialsController> logger,
    LinkGenerator linkGenerator,
    IHttpClientFactory httpClientFactory)
    : ControllerBase
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("PDMS");

    [HttpGet]
    public async Task<IActionResult> GetMaterials([FromQuery] string? category = null, [FromQuery] int pageNo = 1,
        [FromQuery] int pageSize = 10)
    {
        var count = await GetMaterialsCount(category ?? string.Empty);
        var designMaterialDto = new DesignMaterialDto
        {
            MaterialCategory = category ?? string.Empty
        };

        var data = ApiHelper.BuildFormUrlEncodedContent(new SelectDesignMaterialRequestDto
        {
            OperationInfo = new OperationInfoDto { Operator = "6470" },
            MainTable = new DesignMaterialDto
            {
                MaterialCategory = category ?? string.Empty
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
            PageNo = pageNo,
            PageSize = pageSize,
            PagesCount = (int)Math.Ceiling((double)count / pageSize),
            ItemsCount = count,
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
        var content = ApiHelper.BuildFormUrlEncodedContent(data);

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

        throw new BadHttpRequestException($"Failed to get materials count. Keywords:{data.GetKeyParameters()}");
    }
}