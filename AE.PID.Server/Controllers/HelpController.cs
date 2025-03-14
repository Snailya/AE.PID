using AE.PID.Server.Data;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AE.PID.Server.Controllers;

[ApiController]
[Route("api/v{apiVersion:apiVersion}/[controller]")]
[ApiVersion(3)]
public class HelpController(ILogger<HelpController> logger, AppDbContext dbContext, LinkGenerator linkGenerator)
    : ControllerBase
{
    /// <summary>
    ///     获取最新帮助文档。
    /// </summary>
    /// <returns></returns>
    [HttpGet("file/{versionId:int}")]
    public IActionResult GetFile(int versionId)
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "file", versionId.ToString());

        return PhysicalFile(filePath, "application/octet-stream");
    }
}