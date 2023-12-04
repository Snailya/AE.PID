using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PID.Core.Dtos;
using PID.Server.Data;
using PID.Server.Models;
using PID.Server.Services;

namespace PID.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class LibrariesController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly LinkGenerator _linkGenerator;
    private readonly ILogger<LibrariesController> _logger;

    public LibrariesController(ILogger<LibrariesController> logger, AppDbContext dbContext, LinkGenerator linkGenerator)
    {
        _logger = logger;
        _dbContext = dbContext;
        _linkGenerator = linkGenerator;
    }

    [HttpGet]
    public IActionResult GetLibraries()
    {
        var libraries = _dbContext.Libraries.Include(x => x.Versions).ThenInclude(x => x.Items).AsEnumerable().Select(
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
                _linkGenerator.GetUriByAction(HttpContext, nameof(DownloadLibrary), null, new { id = library.Id });
            library.DownloadUrl = string.IsNullOrEmpty(downloadUrl) ? string.Empty : downloadUrl;
        }

        return Ok(libraries);
    }

    [HttpPost("upload")]
    public IActionResult Upload([FromForm] UploadLibraryDto dto)
    {
        // Validate model and handle the file upload
        if (dto?.File == null || Path.GetExtension(dto.File.FileName) != ".vssx")
            return BadRequest("Invalid request. Please provide a vssx file.");

        // validate if the version is larger than the latest version
        var library = _dbContext.Libraries.SingleOrDefault(x => x.Name == dto.Name) ??
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

        _dbContext.Libraries.Update(library);
        _dbContext.SaveChanges();

        return Ok("File uploaded successfully.");
    }

    [HttpDelete("{id:int}")]
    public IActionResult DeleteLibrary([FromRoute] int id)
    {
        var library = _dbContext.Libraries.Include(x => x.Versions).SingleOrDefault(x => x.Id == id);
        if (library == null)
            return NoContent();

        _dbContext.Libraries.Remove(library);
        _dbContext.SaveChanges();

        return NoContent(); // Or handle the case when the file is not found
    }

    [HttpGet("{id:int}/download")]
    public IActionResult DownloadLibrary([FromRoute] int id)
    {
        var library = _dbContext.Libraries.Include(x => x.Versions).SingleOrDefault(x => x.Id == id);
        if (library != null)
            _logger.LogInformation($"Find {library.Name} with {library.Versions.Count} versions.");

        var latestVersion = library?.GetLatestVersion();
        if (latestVersion != null && System.IO.File.Exists(latestVersion.FileName))
            // Return the file as a downloadable response
            return PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(), latestVersion.FileName),
                "application/octet-stream", Path.ChangeExtension(library!.Name, "vssx"), true);

        return NotFound(); // Or handle the case when the file is not found
    }
}