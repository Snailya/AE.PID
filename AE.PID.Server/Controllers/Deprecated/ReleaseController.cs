using AE.PID.Core.DTOs;
using AE.PID.Server.Data;
using Microsoft.AspNetCore.Mvc;

namespace AE.PID.Server.Controllers;

[ApiController]
[Route("api/v{v:apiVersion}/[controller]")]
public class ReleaseController(ILogger<ReleaseController> logger, AppDbContext dbContext, LinkGenerator linkGenerator)
    : ControllerBase
{
    [HttpGet("latest")]
    public IActionResult CheckForUpdate([FromQuery] string currentVersion)
    {
        if (string.IsNullOrEmpty(currentVersion)) return BadRequest("currentVersion is required.");

        // get the version latest version from database
        var latestVersion = dbContext.AppVersions.AsEnumerable().MaxBy(v => new Version(v.Version));

        // 没有新版本
        if (latestVersion == null) return NoContent();
        if (!IsNewVersionAvailable(currentVersion, latestVersion.Version)) return NoContent();

        // return new version
        var downloadUrl = linkGenerator.GetUriByAction(HttpContext, nameof(Download),
            ControllerContext.ActionDescriptor.ControllerName, new { id = latestVersion.Id })!;

        return Ok(new CheckForUpdateResponseDto
        {
            HasUpdate = true,
            LatestVersion = latestVersion!.Version,
            DownloadUrl = downloadUrl,
            ReleaseNotes = latestVersion.ReleaseNotes
        });
    }

    private static bool IsNewVersionAvailable(string currentVersion, string latestVersion)
    {
        // 版本号比较，可以使用 Version 类
        var current = new Version(currentVersion);
        var latest = new Version(latestVersion);
        return latest > current;
    }

    [HttpGet("{versionId:int?}/download")]
    public IActionResult Download([FromRoute] int versionId = 0)
    {
        var version = versionId == 0
            ? dbContext.AppVersions.AsEnumerable().MaxBy(v => new Version(v.Version))
            : dbContext.AppVersions.Find(versionId);

        if (version != null && System.IO.File.Exists(version.PhysicalFile))
            // Return the file as a downloadable response
            return PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(), version.PhysicalFile),
                "application/octet-stream", version.PhysicalFile, true);


        return NotFound(); // Or handle the case when the file is not found
    }
}