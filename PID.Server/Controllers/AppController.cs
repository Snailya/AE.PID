using AE.PID.Server.Data;
using AE.PID.Server.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace AE.PID.Server.Controllers;

[ApiController]
public class AppController(ILogger<AppController> logger, AppDbContext dbContext, LinkGenerator linkGenerator)
    : ControllerBase
{
    [HttpGet("check-version")]
    public IActionResult CheckVersion([FromQuery] string version)
    {
        var latestVersion = dbContext.AppVersions.AsEnumerable().MaxBy(v => new Version(v.Version));

        if (latestVersion != null && new Version(version) < new Version(latestVersion.Version))
            // If the client version is outdated, return information about the latest version
            return Ok(new
            {
                IsUpdateAvailable = true,
                LatestVersion = new
                {
                    latestVersion.Id,
                    latestVersion.Version,
                    latestVersion.ReleaseNotes,
                    DownloadUrl = linkGenerator.GetUriByAction(HttpContext, nameof(Download),
                        ControllerContext.ActionDescriptor.ControllerName, new { id = latestVersion.Id })
                }
            });

        // If the client version is up to date, return a message indicating that
        return Ok(new
        {
            IsUpdateAvailable = false,
            Message = "You have the latest version."
        });
    }

    [HttpGet("download/{id:int}")]
    public IActionResult Download([FromRoute] int id)
    {
        var version = dbContext.AppVersions.Find(id);
        if (version != null && System.IO.File.Exists(version.FileName))
            // Return the file as a downloadable response
            return PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(), version.FileName),
                "application/octet-stream", version.FileName, true);

        return NotFound(); // Or handle the case when the file is not found
    }

    [HttpPost("upload")]
    public IActionResult UploadInstaller([FromForm] UploadInstallerDto dto)
    {
        // Save the uploaded file to a folder
        var filePath = Path.Combine("/opt/pid/data/apps", dto.Installer.FileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            dto.Installer.CopyTo(stream);
        }

        // You can process the version and release note as needed
        var appVersion = dbContext.Add(new AppVersionEntity
        {
            Version = dto.Version,
            ReleaseNotes = dto.ReleaseNotes,
            FileName = filePath
        });
        dbContext.SaveChanges();

        return Ok(new
        {
            DownloadUrl = linkGenerator.GetUriByAction(HttpContext, nameof(Download),
                ControllerContext.ActionDescriptor.ControllerName, new { id = appVersion.Entity.Id })
        });
    }
}