using System.Text.Json;
using AE.PID.Server.DTOs;
using AE.PID.Server.DTOs.PDMS;
using AE.PID.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace AE.PID.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class CategoriesController(
    ILogger<LibrariesController> logger,
    LinkGenerator linkGenerator,
    IHttpClientFactory httpClientFactory)
    : ControllerBase
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("PDMS");

    [HttpGet]
    public async Task<IActionResult> GetCategories([FromQuery] string? name = null)
    {
        var data = ApiHelper.BuildFormUrlEncodedContent(new SelectDesignMaterialCategoryRequestDto
        {
            OperationInfo = new OperationInfoDto { Operator = "6470" },
            MainTable = new DesignMaterialCategoryDto { CategoryName = name ?? "" },
            PageInfo = new PageInfoDto(1, 10000)
        });

        var response = await _client.PostAsync("getModeDataPageList/selectDesignMaterialCategory", data);

        if (!response.IsSuccessStatusCode) return BadRequest("Failed to send form data to the API");

        var responseData = await response.Content.ReadFromJsonAsync<ResponseDto>();
        if (string.IsNullOrEmpty(responseData?.Result)) return NoContent();

        var projects = JsonSerializer
            .Deserialize<IEnumerable<SelectDesignMaterialCategoryResponseItemDto>>(responseData.Result)
            ?.Select(x => x.FromPDMS());
        return Ok(projects);
    }
}