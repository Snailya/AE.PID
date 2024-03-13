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
    [HttpGet]
    public IActionResult GetLibraries()
    {
        var libraries = dbContext.Libraries.Include(x => x.Versions).ThenInclude(x => x.Items).AsEnumerable().Select(
            x =>
                new LibraryDto
                {
                    Id = x.Id, Name = x.Name, Version = x.GetLatestVersion()!.Version, Items = x.GetLatestVersion()!
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

    [HttpPost("upload")]
    public IActionResult Upload([FromForm] UploadLibraryDto dto)
    {
        // Validate model and handle the file upload
        if (Path.GetExtension(dto.File.FileName) != ".vssx")
            return BadRequest("Invalid request. Please provide a vssx file.");

        // validate if the version is larger than the latest version
        var library = dbContext.Libraries.SingleOrDefault(x => x.Name == dto.Name) ??
                      new LibraryEntity { Name = dto.Name };
        var latestVersion = library.GetLatestVersion();
        if (latestVersion != null && new Version(latestVersion.Version) > new Version(dto.Version))
            return BadRequest($"Invalid request. upload version must larger than {latestVersion.Version}");

        // Save the uploaded file to a folder
        var filePath = Path.Combine("/opt/pid/data/libraries", Path.GetFileName(Path.GetTempFileName()));

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            dto.File.CopyTo(stream);
        }

        // You can process the version and release note as needed
        library.Versions.Add(new LibraryVersionEntity
        {
            FileName = filePath, ReleaseNotes = dto.ReleaseNote, Version = dto.Version,
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
    public IActionResult DownloadLibrary([FromRoute] int id)
    {
        var library = dbContext.Libraries.Include(x => x.Versions).SingleOrDefault(x => x.Id == id);
        if (library != null)
            logger.LogInformation($"Find {library.Name} with {library.Versions.Count} versions.");

        var latestVersion = library?.GetLatestVersion();
        if (latestVersion != null && System.IO.File.Exists(latestVersion.FileName))
            // Return the file as a downloadable response
            return PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(), latestVersion.FileName),
                "application/octet-stream", Path.ChangeExtension(library!.Name, "vssx"), true);

        return NotFound(); // Or handle the case when the file is not found
    }
}