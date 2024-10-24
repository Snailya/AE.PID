using System.IO.Packaging;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using AE.PID.Core.DTOs;
using AE.PID.Server.Data;
using AE.PID.Server.DTOs;
using AE.PID.Server.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AE.PID.Server.Controllers;

[ApiController]
[Route("api/v{apiVersion:apiVersion}/[controller]")]
[ApiVersion(3)]
public class StencilsController(ILogger<StencilsController> logger, AppDbContext dbContext, LinkGenerator linkGenerator)
    : ControllerBase
{
    /// <summary>
    ///     获取所有的模具库或者指定id的模具库，用于调试时查看。
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Get([FromQuery] int? id = null)
    {
        if (id == null) return Ok(dbContext.Stencils);
        var stencil = dbContext.Stencils.Find(id);
        if (stencil == null) return NotFound();
        return Ok(stencil);
    }

    /// <summary>
    ///     获取模具库的最新版本，用于客户端比较本地的库版本是否为最新的
    /// </summary>
    /// <param name="status"></param>
    /// <returns></returns>
    [HttpGet("snapshots")]
    public IActionResult GetStencilSnapshots([FromQuery] SnapshotStatus status = SnapshotStatus.Published)
    {
        var snapshots = dbContext.Stencils.Include(x => x.StencilSnapshots).Select(x =>
                x.StencilSnapshots.Where(i => i.Status >= status).OrderByDescending(i => i.CreatedAt).FirstOrDefault())
            .Where(x => x != null).Cast<StencilSnapshot>().ToList();

        var dtos = snapshots.Select(x =>
        {
            dbContext.Entry(x).Reference(i => i.Stencil).Load();
            return new StencilSnapshotDto
            {
                Id = x.Id,
                StencilId = x.StencilId,
                StencilName = x.Stencil.Name,
                DownloadUrl = linkGenerator.GetUriByAction(HttpContext, nameof(Download), null,
                    new { id = x.Id, apiVersion = "3" })!
            };
        });
        return Ok(dtos);
    }

    /// <summary>
    ///     用于下载文件snapshot对应的物理文件。
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("snapshots/{id:int}/file")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Download([FromRoute] int id)
    {
        var snapshot = dbContext.StencilSnapshots.Find(id);
        if (snapshot == null) return NotFound();
        if (!System.IO.File.Exists(snapshot.PhysicalFilePath)) return Problem();

        dbContext.Entry(snapshot).Reference(x => x.Stencil).Load();
        return PhysicalFile(snapshot.PhysicalFilePath,
            "application/octet-stream", Path.ChangeExtension(snapshot.Stencil.Name, "vssx"), true);
    }

    /// <summary>
    ///     变更Snapshot的状态，将草稿状态的模具库调整为发布状态。
    /// </summary>
    /// <param name="id"></param>
    /// <param name="status"></param>
    /// <returns></returns>
    [HttpPatch("snapshots/{id:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult PatchStencilSnapshots([FromRoute] int id,
        [FromBody] SnapshotStatus status = SnapshotStatus.Published)
    {
        var snapshot = dbContext.StencilSnapshots.Find(id);
        if (snapshot == null) return BadRequest();

        snapshot.Status = status;
        snapshot.ModifiedAt = DateTime.Now;
        dbContext.Update(snapshot);
        dbContext.SaveChanges();
        return Ok(snapshot);
    }

    /// <summary>
    ///     通过文件新增或更新模具库。
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Upload([FromForm] UploadStencilDto dto)
    {
        // Validate the model and handle the file upload
        if (Path.GetExtension(dto.File.FileName) != ".vssx")
            return BadRequest("Invalid request. Please provide a vssx file.");

        // save the file to local storage
        var physicalFilePath = SaveFile(dto);

        // build up stencil
        var name = string.IsNullOrEmpty(dto.Name) ? Path.GetFileNameWithoutExtension(dto.File.FileName) : dto.Name;
        var stencil = dbContext.Stencils.SingleOrDefault(x => x.Name == name) ?? new Stencil { Name = name };
        var snapshot = new StencilSnapshot
        {
            PhysicalFilePath = physicalFilePath,
            Description = dto.ReleaseNote,
            Status = SnapshotStatus.Draft,
            Stencil = stencil
        };

        // update the snapshot linked to the stencil
        snapshot.MasterContentSnapshots =
            snapshot.MasterContentSnapshots.Concat(BuildMasters(physicalFilePath)).ToList();
        stencil.StencilSnapshots.Add(snapshot);

        dbContext.Stencils.Update(stencil);
        dbContext.SaveChanges();

        return Ok(snapshot);
    }

    private static string SaveFile(UploadStencilDto dto)
    {
        // Save the uploaded file to a folder
        var filePath = Path.Combine(Constants.StencilPath, Path.GetFileName(Path.GetTempFileName()));

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            dto.File.CopyTo(stream);
        }

        return filePath;
    }

    private List<MasterContentSnapshot> BuildMasters(string filePath)
    {
        var snapshots = new List<MasterContentSnapshot>();

        using var package = Package.Open(filePath, FileMode.Open, FileAccess.Read);
        var mastersPackagePart = VisioXmlWrapper.GetMastersPart(package);
        var styles = VisioXmlWrapper.GetStyles(package).ToList();

        // Loop through masters part to get

        using var partXmlReader = XmlReader.Create(mastersPackagePart.GetStream());
        foreach (var masterElement in XElement.Load(partXmlReader).Elements())
        {
            var baseId = masterElement.Attribute("BaseID")!.Value;
            var name = masterElement.Attribute("NameU")!.Value;

            var master = dbContext.Masters.SingleOrDefault(x => x.BaseId == baseId) ??
                         new Master { BaseId = baseId, Name = name };

            // 更新master名称
            if (master.Id != 0)
                if (name != master.Name)
                {
                    master.Name = name;
                    master.ModifiedAt = DateTime.Now;
                }

            // 加载导航属性
            dbContext.Entry(master).Collection(a => a.MasterContentSnapshots).Load();

            // 检查是否存在该对象
            var uniqueId = masterElement.Attribute("UniqueID")!.Value;
            if (master.MasterContentSnapshots.SingleOrDefault(x => x.UniqueId == uniqueId) is { } snapshot)
            {
                snapshots.Add(snapshot);
                continue;
            }

            // 如果对象不存在，读取关联的master.xml文件
            var masterDocument =
                XmlHelper.GetDocumentFromPart(
                    VisioXmlWrapper.GetMasterPartByMasterId(package, int.Parse(masterElement.Attribute("ID").Value)));
            var shapeElement = masterDocument.XPathSelectElement("/main:MasterContents/main:Shapes/main:Shape",
                VisioXmlWrapper.NamespaceManager);

            var lineStyleName = styles.Single(x => x.Id == int.Parse(shapeElement.Attribute("LineStyle").Value))
                .Name;
            var fillStyleName = styles.Single(x => x.Id == int.Parse(shapeElement.Attribute("FillStyle").Value))
                .Name;
            var textStyleName = styles.Single(x => x.Id == int.Parse(shapeElement.Attribute("TextStyle").Value))
                .Name;

            // 添加为新的对象
            var masterContentSnapshot = new MasterContentSnapshot
            {
                Status = SnapshotStatus.Draft,
                BaseId = baseId,
                UniqueId = uniqueId,
                LineStyleName = lineStyleName,
                FillStyleName = fillStyleName,
                TextStyleName = textStyleName,
                MasterElement = masterElement.ToString(SaveOptions.DisableFormatting),
                MasterDocument = masterDocument.ToString(SaveOptions.DisableFormatting),
                Master = master
            };

            snapshots.Add(masterContentSnapshot);
        }

        return snapshots;
    }
}