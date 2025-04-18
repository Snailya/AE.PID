﻿using System.IO.Packaging;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using AE.PID.Core;
using AE.PID.Server.Data;
using AE.PID.Server.DTOs;
using AE.PID.Server.Helpers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AE.PID.Server.Apis;

public static class VisioStencilApi
{
    public static RouteGroupBuilder MapVisioStencilEndpoints(this RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("stencils/snapshots", GetLatestSnapshots)
            .WithDescription("获取模具的最新版本。在客户端中，比较本地模具与服务器的版本以判断是否需要更新本地文件。")
            .WithTags("Visio模具");
        groupBuilder.MapGet("stencils/snapshots/{id:int}/file", DownloadStencil)
            .WithDescription("下载模具的物理文件。")
            .WithTags("Visio模具")
            .WithName(nameof(DownloadStencil));

        groupBuilder.MapPost("stencils", UploadStencil)
            .DisableAntiforgery()
            .WithDescription("上传新版本的模具")
            .WithTags("Visio模具");
        groupBuilder.MapPost("stencils/snapshots/{id:int}/update-status", UpdateStatus)
            .WithDescription("当新版本的模具被上传后，其默认状态为草稿版本，通过变更其状态为发布，是模具可以被普通通道的用户发现。")
            .WithTags("Visio模具");
        // groupBuilder.MapPost("masters/snapshots/update-status-by-stencil-snapshot-id",
        //         UpdateMasterStatusByStencilSnapshotId)
        //     .WithTags("Visio模具");
        // groupBuilder.MapPost("masters/snapshots/{id:int}/update-status", UpdateMasterStatus)
        //     .WithTags("Visio模具");

        return groupBuilder;
    }

    private static Results<Ok<IEnumerable<StencilSnapshotDto>>, NotFound> GetLatestSnapshots(HttpContext context,
        LinkGenerator linkGenerator, AppDbContext dbContext,
        [FromQuery] SnapshotStatus status = SnapshotStatus.Published)
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
        LinkGenerator linkGenerator, AppDbContext dbContext, [FromRoute] int? id = null)
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
        [FromBody] SnapshotStatus status = SnapshotStatus.Published)
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

    private static Results<Ok, Ok<ICollection<MasterContentSnapshot>>, ProblemHttpResult>
        UpdateMasterStatusByStencilSnapshotId(HttpContext context,
            LinkGenerator linkGenerator, AppDbContext dbContext, [FromQuery] int stencilSnapshotId,
            [FromBody] SnapshotStatus status = SnapshotStatus.Published)
    {
        var stencilSnapshot = dbContext.StencilSnapshots.Find(stencilSnapshotId);
        if (stencilSnapshot == null) return TypedResults.Problem();

        dbContext.Entry(stencilSnapshot).Collection(x => x.MasterContentSnapshots).Load();
        var count = stencilSnapshot.MasterContentSnapshots.Count(x => x.Status != status);
        if (count == 0) return TypedResults.Ok();

        foreach (var snapshot in stencilSnapshot.MasterContentSnapshots)
        {
            snapshot.Status = status;
            snapshot.ModifiedAt = DateTime.Now;
        }

        dbContext.Update(stencilSnapshot);
        dbContext.SaveChanges();

        dbContext.Entry(stencilSnapshot).Reference(x => x.Stencil).Load();
        // logger.LogInformation(
        //     "Status updated. Target: {StencilName}.{SnapshotId}, Count: {Count}, Current: {CurrentValue}",
        //     stencilSnapshot.Stencil.Name, stencilSnapshotId, count, status);

        return TypedResults.Ok(stencilSnapshot.MasterContentSnapshots);
    }

    private static Results<Ok<MasterContentSnapshot>, ProblemHttpResult> UpdateMasterStatus(HttpContext context,
        LinkGenerator linkGenerator, AppDbContext dbContext,
        [FromRoute] int id,
        [FromBody] SnapshotStatus status = SnapshotStatus.Published)
    {
        var snapshot = dbContext.MasterContentSnapshots.Find(id);
        if (snapshot == null) return TypedResults.Problem();

        if (snapshot.Status == status) return TypedResults.Ok(snapshot);

        var previousStatus = snapshot.Status;
        snapshot.Status = status;
        snapshot.ModifiedAt = DateTime.Now;
        dbContext.Update(snapshot);
        dbContext.SaveChanges();

        dbContext.Entry(snapshot).Reference(x => x.Master).Load();
        // logger.LogInformation(
        //     "Status updated. Target: {MasterName}.{MasterSnapshotId}, Previous: {PreviousValue}, Current: {CurrentValue}",
        //     snapshot.Master.Name, id, previousStatus,
        //     snapshot.Status);

        return TypedResults.Ok(snapshot);
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