using System.ComponentModel;
using System.IO.Packaging;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using AE.PID.Core;
using AE.PID.Server.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AE.PID.Server.Apis;

public static class VisioStencilApi
{
    public static RouteGroupBuilder MapVisioStencilEndpoints(this RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("stencils/snapshots", GetLatestSnapshots)
            .WithTags("Visio模具")
            .WithDescription("获取所有模具的最新可用版本快照。");
        groupBuilder.MapGet("stencils/snapshots/{id:int}/file", DownloadStencil)
            .WithTags("Visio模具")
            .WithName(nameof(DownloadStencil))
            .WithDescription("下载指定版本快照的Visio模具文件（.vssx）。");

        groupBuilder.MapPost("stencils", UploadStencil)
            .DisableAntiforgery()
            .WithTags("Visio模具")
            .WithDescription("上传新版本的模具");
        groupBuilder.MapPost("stencils/snapshots/{id:int}/update-status", UpdateStatus)
            .WithTags("Visio模具")
            .WithDescription("当新版本的模具被上传后，其默认状态为草稿版本，通过变更其状态为发布，是模具可以被普通通道的用户发现。");

        return groupBuilder;
    }

    private static Results<Ok<IEnumerable<StencilSnapshotDto>>, NotFound> GetLatestSnapshots(HttpContext context,
        LinkGenerator linkGenerator, AppDbContext dbContext,
        [FromQuery] [Description("快照状态")] SnapshotStatus status = SnapshotStatus.Published)
    {
        var snapshots = dbContext.Stencils.Include(x => x.StencilSnapshots)
            .Select(x =>
                x.StencilSnapshots
                    .Where(i => i.Status >= status)
                    .OrderByDescending(i => i.CreatedAt)
                    .FirstOrDefault()
            )
            .Where(x => x != null)
            .Cast<StencilSnapshot>()
            .ToList();

        return TypedResults.Ok(snapshots.Select(x =>
        {
            dbContext.Entry(x).Reference(i => i.Stencil).Load();
            var url = linkGenerator.GetUriByName(context, nameof(DownloadStencil),
                new { id = x.Id, apiVersion = "3" }) ?? string.Empty;
            return MapToDto(x, url);
        }));
    }

    private static Results<PhysicalFileHttpResult, NotFound, ProblemHttpResult> DownloadStencil(HttpContext context,
        LinkGenerator linkGenerator, AppDbContext dbContext, [FromRoute] [Description("快照ID")] int? id = null)
    {
        var snapshot = dbContext.StencilSnapshots
            .Include(s => s.Stencil)
            .FirstOrDefault(s => s.Id == id);

        if (snapshot == null) return TypedResults.NotFound();
        if (!File.Exists(snapshot.PhysicalFilePath)) return TypedResults.Problem();

        return TypedResults.PhysicalFile(snapshot.PhysicalFilePath,
            "application/octet-stream", Path.ChangeExtension(snapshot.Stencil.Name, "vssx"));
    }

    private static Results<Ok<StencilSnapshotDto>, ProblemHttpResult> UploadStencil(HttpContext context,
        LinkGenerator linkGenerator, AppDbContext dbContext, [FromForm] UploadStencilDto dto)
    {
        // Validate the model and handle the file upload
        if (Path.GetExtension(dto.File.FileName) != ".vssx")
            return TypedResults.Problem("Invalid request. Please provide a vssx file.");

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
            snapshot.MasterContentSnapshots.Concat(BuildMasters(dbContext, physicalFilePath)).ToList();
        stencil.StencilSnapshots.Add(snapshot);

        dbContext.Stencils.Update(stencil);
        dbContext.SaveChanges();

        var url = linkGenerator.GetUriByName(context, nameof(DownloadStencil),
            new { id = snapshot.Id, apiVersion = "3" }) ?? string.Empty;
        return TypedResults.Ok(MapToDto(snapshot, url));
    }

    private static Results<Ok<StencilSnapshotDto>, NotFound, ProblemHttpResult> UpdateStatus(HttpContext context,
        LinkGenerator linkGenerator, AppDbContext dbContext, [FromRoute] int id,
        [FromBody] [Description("快照状态")] SnapshotStatus status = SnapshotStatus.Published)
    {
        var snapshot = dbContext.StencilSnapshots.Include(x => x.MasterContentSnapshots).Include(x => x.Stencil)
            .SingleOrDefault(x => x.Id == id);
        if (snapshot == null) return TypedResults.NotFound();

        // 更新模具状态
        var count = snapshot.MasterContentSnapshots.Count(x => x.Status != status);
        if (count == 0) return TypedResults.Ok(MapToDto(snapshot, ""));

        foreach (var content in snapshot.MasterContentSnapshots)
        {
            content.Status = status;
            content.ModifiedAt = DateTime.Now;
        }

        // 更新模具库状态
        snapshot.Status = status;
        snapshot.ModifiedAt = DateTime.Now;

        dbContext.Update(snapshot);
        dbContext.SaveChanges();

        return TypedResults.Ok(MapToDto(snapshot, ""));
    }


    private static string SaveFile(UploadStencilDto dto)
    {
        // Save the uploaded file to a folder
        var filePath = Path.Combine(PathConstants.StencilPath, Path.GetFileName(Path.GetTempFileName()));

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            dto.File.CopyTo(stream);
        }

        return filePath;
    }

    private static List<MasterContentSnapshot> BuildMasters(AppDbContext dbContext, string filePath)
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

    private static StencilSnapshotDto MapToDto(StencilSnapshot snapshot, string downloadUrl)
    {
        return new StencilSnapshotDto
        {
            StencilId = snapshot.StencilId,
            StencilName = snapshot.Stencil.Name,
            DownloadUrl = downloadUrl,
            Id = snapshot.Id,
            Description = snapshot.Description
        };
    }
}