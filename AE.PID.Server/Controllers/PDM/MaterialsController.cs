using System.Text;
using System.Text.Json;
using AE.PID.Server.Core;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AE.PID.Server.Controllers;

[ApiController]
[Route("api/v{apiVersion:apiVersion}/[controller]")]
[ApiVersion(3)]
public class MaterialsController(IMaterialService materialService)
    : ControllerBase
{
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
        try
        {
            var material = await materialService.GetMaterialByCodeAsync(userId, code);
            return Ok(material);
        }
        catch (BadHttpRequestException e)
        {
            return BadRequest(e.Message);
        }
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
        try
        {
            var results = await materialService.GetMaterialsAsync(userId, category, s, pageNo, pageSize);
            if (results is null) return NoContent();
            return Ok(results);
        }
        catch (BadHttpRequestException e)
        {
            return BadRequest(e.Message);
        }
    }

    /// <summary>
    ///     获取物料数据的Json文件。
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="category"></param>
    /// <param name="s"></param>
    /// <param name="pageNo"></param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    [HttpGet("file")]
    public async Task<IActionResult> GetMaterialsAsFile([FromHeader(Name = "User-ID")] string userId,
        [FromQuery] string? category = null, [FromQuery] string? s = null,
        [FromQuery] int pageNo = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var materials = await materialService.GetFlattenMaterialsAsync(userId, category, s, pageNo, pageSize);

            if (materials == null) return NoContent();

            var json = JsonSerializer.Serialize(materials);
            var byteArray = Encoding.UTF8.GetBytes(json);
            var stream = new MemoryStream(byteArray);

            return File(stream, "application/json", $"category={category}&no={pageNo}&size={pageSize}.json");
        }
        catch (BadHttpRequestException e)
        {
            return BadRequest(e.Message);
        }
    }
}