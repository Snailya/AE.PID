using AE.PID.Core.DTOs;
using AE.PID.Server.Data;
using AE.PID.Server.DTOs;
using AE.PID.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AE.PID.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class LibrariesController(
    ILogger<LibrariesController> logger,
    AppDbContext dbContext,
    LinkGenerator linkGenerator)
    : ControllerBase
{
    /// <summary>
    /// Get info for all library. For client.
    /// </summary>
    /// <param name="involvePrerelease"></param>
    /// <returns></returns>
    [HttpGet]
    public IActionResult GetLibraries([FromQuery]bool involvePrerelease = false)
    {
        var libraries = dbContext.Libraries.Include(x => x.Versions).ThenInclude(x => x.Items).AsEnumerable()
            .Select(
            x =>
                new LibraryDto
                {
                    Id = x.Id, Name = x.Name, Version = x.GetLatestVersion(involvePrerelease)?.Version??string.Empty, Items = x.GetLatestVersion(involvePrerelease)?
                        .Items.Select(i => new LibraryItemDto
                        {
                            Name = i.Name, BaseId = i.BaseId, UniqueId = i.UniqueId
                        }).ToList()
                }).ToList();

        foreach (var library in libraries)
        {
            var downloadUrl =
                linkGenerator.GetUriByAction(HttpContext, nameof(DownloadLibrary), null, new { id = library.Id });
            library.DownloadUrl = string.IsNullOrEmpty(downloadUrl) ? string.Empty : downloadUrl;
        }

        return Ok(libraries);
    }

    /// <summary>
    /// Get library info by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id:int}")]
    public IActionResult GetLibrary([FromRoute] int id)
    {
        var library = dbContext.Libraries.Include(x=>x.Versions).SingleOrDefault(x => x.Id == id);
        if (library == null)
            return NoContent();
        return Ok(library);
    }
    
    /// <summary>
    /// Update version state by version id.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="versionId"></param>
    /// <returns></returns>
    [HttpGet("{id:int}/versions/{versionId:int}")]
    public IActionResult GetVersion([FromRoute] int id, [FromRoute] int versionId)
    {
        var version = dbContext.Libraries.Where(x => x.Id==id)
            .SelectMany(x=>x.Versions)
            .Include(x=>x.Items)
            .SingleOrDefault(x => x.Id == versionId);
 
        return Ok(version);
    }
    
    /// <summary>
    /// Update version state by version id.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="versionId"></param>
    /// <returns></returns>
    [HttpPost("{id:int}/versions/{versionId:int}")]
    public IActionResult Release([FromRoute] int id, [FromRoute] int versionId)
    {
        var library = dbContext.Libraries.Include(x => x.Versions).SingleOrDefault(x => x.Id == id);
        if (library == null)
            return BadRequest();
        var version = library.Versions.SingleOrDefault(x => x.Id == versionId);
        if (version == null)
            return BadRequest();
        version.IsReleased = true;
        dbContext.Libraries.Update(library);
        dbContext.SaveChanges();
        return Ok(version);
    }
    
    /// <summary>
    /// Update version state by version id.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="versionId"></param>
    /// <returns></returns>
    [HttpDelete("{id:int}/versions/{versionId:int}")]
    public IActionResult DeleteVersion([FromRoute] int id, [FromRoute] int versionId)
    {
        var library = dbContext.Libraries.Include(x => x.Versions).SingleOrDefault(x => x.Id == id);
        if (library == null)
            return BadRequest();
        var version = library.Versions.SingleOrDefault(x => x.Id == versionId);
        if (version == null)
            return NoContent();

        library.Versions.Remove(version);
        dbContext.Libraries.Update(library);
        dbContext.SaveChanges();
        return NoContent();
    }
    
    /// <summary>
    /// Upload library.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("upload")]
    public IActionResult Upload([FromForm] UploadLibraryDto dto)
    {
        // Validate model and handle the file upload
        if (Path.GetExtension(dto.File.FileName) != ".vssx")
            return BadRequest("Invalid request. Please provide a vssx file.");

        var name = Path.GetFileNameWithoutExtension(dto.File.FileName);
        
        var library = dbContext.Libraries.Include(x=>x.Versions).SingleOrDefault(x => x.Name == name) ??
                      new LibraryEntity { Name = name };
        var currentVersion = new Version(0,1,0,0);

        var latestLibraryVersion = library.GetLatestVersion(true);
        logger.LogInformation("Found latest version {V}", latestLibraryVersion);
        if (latestLibraryVersion != null)
        {
            var latestVersion = new Version(latestLibraryVersion.Version);
            if (dto.IsMinorUpdate)
            {
                currentVersion = new Version(latestVersion.Major, latestVersion.Minor + 1, 0,
                    0);
            }
            else
            {
                currentVersion = new Version(latestVersion.Major, latestVersion.Minor, latestVersion.Build,
                    latestVersion.Revision + 1);
            }
        }
        
        logger.LogInformation("Set current version {V}", currentVersion);

        // Save the uploaded file to a folder
        var filePath = Path.Combine("/opt/pid/data/libraries", Path.GetFileName(Path.GetTempFileName()));

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            dto.File.CopyTo(stream);
        }

        // You can process the version and release note as needed
        library.Versions.Add(new LibraryVersionEntity
        {
            FileName = filePath, ReleaseNotes = dto.ReleaseNote, Version = currentVersion.ToString(),
            Items = OpenXmlService.GetItems(filePath).ToList()
        });

        dbContext.Libraries.Update(library);
        dbContext.SaveChanges();

        return Ok("File uploaded successfully.");
    }

    [HttpDelete("{id:int}")]
    public IActionResult DeleteLibrary([FromRoute] int id)
    {
        var library = dbContext.Libraries.Include(x => x.Versions).SingleOrDefault(x => x.Id == id);
        if (library == null)
            return NoContent();

        dbContext.Libraries.Remove(library);
        dbContext.SaveChanges();

        return NoContent(); // Or handle the case when the file is not found
    }

    [HttpGet("{id:int}/download")]
    public IActionResult DownloadLibrary([FromRoute] int id, bool involvePrerelease = false)
    {
        var library = dbContext.Libraries.Include(x => x.Versions).SingleOrDefault(x => x.Id == id);
        if (library != null)
            logger.LogInformation($"Find {library.Name} with {library.Versions.Count} versions.");

        var latestVersion = library?.GetLatestVersion(involvePrerelease);
        if (latestVersion != null && System.IO.File.Exists(latestVersion.FileName))
            // Return the file as a downloadable response
            return PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(), latestVersion.FileName),
                "application/octet-stream", Path.ChangeExtension(library!.Name, "vssx"), true);

        return NotFound(); // Or handle the case when the file is not found
    }
}