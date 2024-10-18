using System.Text.RegularExpressions;
using AE.PID.Core.DTOs;
using AE.PID.Server.Data;
using AE.PID.Server.DTOs;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AE.PID.Server.Controllers;

[ApiController]
[Route("api/v{apiVersion:apiVersion}/[controller]")]
[ApiVersion(3)]
public partial class AppController(ILogger<AppController> logger, AppDbContext dbContext, LinkGenerator linkGenerator)
    : ControllerBase
{
    /// <summary>
    ///     获取最新的程序信息。
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public IActionResult GetCurrentApp()
    {
        var version = dbContext.AppVersions.AsEnumerable().MaxBy(x => new Version(x.Version));

        if (version == null) return NoContent();

        return Ok(new AppVersionDto
            {
                Version = version.Version,
                DownloadUrl = linkGenerator.GetUriByAction(HttpContext, nameof(Download),
                                  ControllerContext.ActionDescriptor.ControllerName,
                                  new { id = version.Id, apiVersion = "3" }) ??
                              string.Empty,
                ReleaseNotes = version.ReleaseNotes
            }
        );
    }

    /// <summary>
    ///     下载安装包。
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("download/{id:int?}")]
    public IActionResult Download([FromRoute] int id = 0)
    {
        var version = id == 0
            ? dbContext.AppVersions.AsEnumerable().MaxBy(v => new Version(v.Version))
            : dbContext.AppVersions.Find(id);

        if (version != null && System.IO.File.Exists(version.PhysicalFile))
        {
            var fileName = Path.GetFileName(version.PhysicalFile);
            // Return the file as a downloadable response
            return PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(), version.PhysicalFile),
                "application/octet-stream", fileName, true);
        }

        return NotFound(); // Or handle the case when the file is not found
    }

    /// <summary>
    ///     上传安装包。
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost]
    [RequestSizeLimit(52428800)]
    public IActionResult UploadInstaller([FromForm] UploadInstallerDto dto)
    {
        var filePath = Path.Combine(Constants.InstallerPath, dto.Installer.FileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            dto.Installer.CopyTo(stream);
        }

        // You can process the version and release note as needed
        var versionStr = MyRegex().Match(Path.GetFileNameWithoutExtension(dto.Installer.FileName)).Value;
        logger.LogInformation("Uploaded installer version {Version}", versionStr);

        // check if already exist
        if (dbContext.AppVersions.Any(x => x.Version == versionStr))
            return BadRequest();

        var version = new AppVersion
        {
            Version = versionStr,
            ReleaseNotes = dto.ReleaseNotes,
            PhysicalFile = filePath
        };
        dbContext.AppVersions.Add(version);
        dbContext.SaveChanges();

        return Ok(new
        {
            DownloadUrl = linkGenerator.GetUriByAction(HttpContext, nameof(Download),
                ControllerContext.ActionDescriptor.ControllerName, new { id = version.Id, apiVersion = "3" })
        });
    }

    [GeneratedRegex("[\\d.]+")]
    private static partial Regex MyRegex();
}