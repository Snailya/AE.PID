using System.Text;
using System.Text.Json;
using AE.PID.Core.DTOs;
using AE.PID.Server.Data;
using AE.PID.Server.DTOs;
using AE.PID.Server.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AE.PID.Server.Controllers;

[ApiController]
[ApiVersion(1, Deprecated = true)]
[ApiVersion(2)]
public class LibrariesController(
    ILogger<LibrariesController> logger,
    AppDbContext dbContext,
    LinkGenerator linkGenerator)
    : ControllerBase
{
    /// <summary>
    ///     Get info for all libraries. For client.
    /// </summary>
    /// <param name="involvePrerelease"></param>
    /// <returns></returns>
    [HttpGet("[controller]")]
    public IActionResult GetLibraries([FromQuery] bool involvePrerelease = false)
    {
        var libraries = dbContext.Libraries
            .Include(x => x.Versions.OrderByDescending(v => v.Version))
            .ThenInclude(x => x.Items)
            .AsEnumerable()
            .Select(
                x =>
                {
                    var dto = new LibraryDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Version = string.Empty,
                        Items = []
                    };

                    // if it is an empty library
                    var latestVersion = x.GetLatestVersion(involvePrerelease);
                    if (latestVersion == null) return dto;

                    // if it is not
                    dto.Version = latestVersion.Version;
                    dto.Items = latestVersion.Items.Select(i => new LibraryItemDto
                    {
                        Name = i.Name,
                        BaseId = i.BaseId,
                        UniqueId = i.UniqueId
                    }).ToList();

                    return dto;
                }
            ).ToList();

        foreach (var library in libraries)
        {
            var downloadUrl =
                linkGenerator.GetUriByAction(HttpContext, nameof(DownloadLibrary), null, new { id = library.Id });
            library.DownloadUrl = string.IsNullOrEmpty(downloadUrl) ? string.Empty : downloadUrl;
        }

        return Ok(libraries);
    }

    /// <summary>
    ///     Get library info by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("[controller]/{id:int}")]
    public IActionResult GetLibrary([FromRoute] int id)
    {
        var library = dbContext.Libraries
            .Include(x => x.Versions.OrderByDescending(v => v.Version))
            .SingleOrDefault(x => x.Id == id);
        if (library == null)
            return NoContent();
        return Ok(library);
    }

    /// <summary>
    ///     Upload library.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("api/v{v:apiVersion}/[controller]")]
    public IActionResult Upload([FromForm] UploadLibraryDto dto)
    {
        // Validate the model and handle the file upload
        if (Path.GetExtension(dto.File.FileName) != ".vssx")
            return BadRequest("Invalid request. Please provide a vssx file.");

        var name = Path.GetFileNameWithoutExtension(dto.File.FileName);

        var library = dbContext.Libraries
                          .Include(x => x.Versions)
                          .SingleOrDefault(x => x.Name == name) ??
                      new LibraryEntity { Name = name };
        var currentVersion = new Version(0, 1, 0, 0);

        var latestLibraryVersion = library.GetLatestVersion(true);
        logger.LogInformation("Found latest version {V}", latestLibraryVersion);
        if (latestLibraryVersion != null)
        {
            var latestVersion = new Version(latestLibraryVersion.Version);
            if (dto.IsMinorUpdate)
                currentVersion = new Version(latestVersion.Major, latestVersion.Minor + 1, 0,
                    0);
            else
                currentVersion = new Version(latestVersion.Major, latestVersion.Minor, latestVersion.Build,
                    latestVersion.Revision + 1);
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

        CreateCheatSheet(true);

        return Ok("File uploaded successfully.");
    }

    [HttpPost]
    [Route("api/v{v:apiVersion}/[controller]/{id:int}/delete")]
    public IActionResult DeleteLibrary([FromRoute] int id)
    {
        var library = dbContext.Libraries.Include(x => x.Versions).SingleOrDefault(x => x.Id == id);
        if (library == null)
            return NoContent();

        dbContext.Libraries.Remove(library);
        dbContext.SaveChanges();

        logger.LogInformation("Delete library {Id} from database", id);
        return NoContent(); // Or handle the case when the file is not found
    }

    /// <summary>
    ///     Update version state by version id.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="versionId"></param>
    /// <returns></returns>
    [HttpGet("[controller]/{id:int}/versions/{versionId:int}")]
    public IActionResult GetVersion([FromRoute] int id, [FromRoute] int versionId)
    {
        var version = dbContext.Libraries.Where(x => x.Id == id)
            .SelectMany(x => x.Versions)
            .Include(x => x.Items)
            .SingleOrDefault(x => x.Id == versionId);

        return Ok(version);
    }

    /// <summary>
    ///     Update version state by version id.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="versionId"></param>
    /// <returns></returns>
    [HttpPatch("[controller]/{id:int}/versions/{versionId:int}")]
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

        CreateCheatSheet();

        return Ok(version);
    }

    /// <summary>
    ///     Update version state by version id.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="versionId"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("api/v{v:apiVersion}/[controller]/{id:int}/versions/{versionId:int}/delete")]
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

        // update the cheat sheet, this should be refactored to speed up if in need.
        CreateCheatSheet(true);
        CreateCheatSheet();

        return NoContent();
    }

    [HttpGet("[controller]/{id:int}/download")]
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

    /// <summary>
    ///     Update version state by version id.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="versionId"></param>
    /// <returns></returns>
    [HttpGet("[controller]/{id:int}/versions/{versionId:int}/items")]
    public IActionResult GetVersionItems([FromRoute] int id, [FromRoute] int versionId)
    {
        var version = dbContext.Libraries.Where(x => x.Id == id)
            .SelectMany(x => x.Versions)
            .Include(x => x.Items)
            .SingleOrDefault(x => x.Id == versionId);

        if (version == null) return NoContent();

        return Ok(version.Items.Select(x => x.ToDetailedLibraryItemDto()).ToList());
    }

    [HttpGet("[controller]/cheatsheet")]
    public IActionResult GetCheatSheet(bool involvePrerelease = false)
    {
        var filePath = involvePrerelease
            ? Path.Combine("/opt/pid/data/libraries", ".cheatsheet-pre")
            : Path.Combine("/opt/pid/data/libraries", ".cheatsheet");

        if (!System.IO.File.Exists(filePath))
            CreateCheatSheet(involvePrerelease);

        return PhysicalFile(filePath, "application/octet-stream", ".cheatsheet", true);
    }



    /// <summary>
    ///     Persist latest items into a local file.
    /// </summary>
    /// <param name="involvePrerelease"></param>
    private void CreateCheatSheet(bool involvePrerelease = false)
    {
        var items = Helper.PopulatesCheatSheetItems(dbContext,involvePrerelease).ToList();
        var jsonString = JsonSerializer.Serialize(items);

        var filePath = Path.Combine("/opt/pid/data/libraries", involvePrerelease ? ".cheatsheet-pre" : ".cheatsheet");
        System.IO.File.WriteAllText(filePath, jsonString, Encoding.UTF8);
    }
}