using System.IO.Packaging;
using System.Text.Json;
using AE.PID.Core;
using AE.PID.Server.Data;
using AE.PID.Server.DTOs;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AE.PID.Server.Controllers;

[ApiController]
[Route("api/v{apiVersion:apiVersion}/[controller]")]
[ApiVersion(3)]
public class DocumentsController(
    ILogger<DocumentsController> logger,
    AppDbContext dbContext,
    IDocumentService documentService)
    : ControllerBase
{
    /// <summary>
    ///     更新文档模具。
    ///     2025.02.06: 由于Refit不支持不咋结构数组作为Form的一部分，此处将原来的MasterDto[]? 修改为string?，然后再反序列化。
    /// </summary>
    /// <param name="status"></param>
    /// <param name="file"></param>
    /// <param name="data">
    ///     The ids of the masters that need to perform updating
    /// </param>
    /// <returns></returns>
    [HttpPost("update")]
    public async Task<IActionResult> Update(IFormFile file, [FromForm] string? data = null,
        [FromQuery] SnapshotStatus status = SnapshotStatus.Published)
    {
        var items = data != null ? JsonSerializer.Deserialize<MasterDto[]>(data) : null;

        // do the update
        try
        {
            logger.LogInformation("Received update request from ip address {IP}, update mode: {Mode}", GetClientIp(),
                items == null ? "completely" : "by id");

            var filePath = await DoUpdate(file, items, status);

            // return physical file
            return new PhysicalFileResult(filePath, "application/octet-stream");
        }
        catch (Exception e)
        {
            logger.LogError(e, "File updated failed with error.");

            return BadRequest(e.Message);
        }
    }

    /// <summary>
    ///     以上传文件的方式更新文件的文档模具。
    /// </summary>
    /// <param name="requestDto"></param>
    /// <param name="status"></param>
    /// <returns></returns>
    [HttpPost("update/file")]
    public async Task<IActionResult> Update(DocumentMasterUpdateRequestDto requestDto,
        [FromQuery] SnapshotStatus status = SnapshotStatus.Published
    )
    {
        try
        {
            var filePath = await DoUpdate(requestDto.File, requestDto.Items, status);

            // 2025.02.03: add default filename
            return PhysicalFile(filePath, "application/octet-stream", requestDto.File.FileName);
        }
        catch (Exception e)
        {
            logger.LogError(e, "File updated failed with error.");

            return BadRequest(e.Message);
        }
    }

    private async Task<string> DoUpdate(IFormFile file, MasterDto[]? items, SnapshotStatus status)
    {
        // 2025.02.03： 由于System.IO.Packaging的Package.Open方法必须读取本地的文件，所以这里需要对传入的字节流进行本地缓存。这样做也有利于事后回溯。
        // 此处存储时没有使用文件原本的名称，而是使用时间戳，原因是如果API请求来自于Windows系统，并且使用的是FullPath，通过Path.GetFileName方法获取文件名的时候，还必须替换其中的“\”字符，否则在Linux中由于分隔符的不同，获取结果仍为完整路径，引发后面报错。
        var filePath = Path.ChangeExtension(Path.Combine(Constants.TmpPath, DateTime.Now.ToString("yyyyMMddHHmmssfff")),
            "vsdx");

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        logger.LogInformation("Received file {FileName} from ip address {IP} cached at {Path}.", file.FileName,
            GetClientIp(), filePath);

        // 2025.02.05: 获得需要更新的Master信息
        using var visioPackage = Package.Open(filePath, FileMode.Open, FileAccess.ReadWrite);

        logger.LogInformation("Starting update of {FileName}...", filePath);

        // update styles
        documentService.UpdateStyles(visioPackage);

        // do update
        var documentMasters = documentService.GetDocumentMasters(visioPackage);

        var index = 0;
        var total = items?.Length ?? documentMasters.Length;

        foreach (var source in documentMasters)
        {
            // 2025.02.05: 如果制定了更新列表，需要检查当前的master是不是在更新列表之中，如果不在的话，则跳过
            if (items != null && items.All(x => x.UniqueId != source.UniqueId)) continue;

            logger.LogInformation("Processing {MasterName} ({Index} / {Total})... ", source.Name, ++index,
                total);

            var target = await GetMasterContentSnapshotByBaseId(source.BaseId, status);

            if (target is not null)
                documentService.UpdateMaster(visioPackage, source.UniqueId, target);
        }

        // 添加一个重新计算公式的事件，如果没有的话
        XmlHelper.RecalculateDocument(visioPackage);

        visioPackage.Close();

        logger.LogInformation("Finished update of {FileName}", filePath);

        return filePath;
    }

    /// <summary>
    ///     Get the latest master content snapshot by BaseID.
    /// </summary>
    /// <param name="baseId"></param>
    /// <param name="status"></param>
    /// <returns></returns>
    private async Task<MasterContentSnapshot?> GetMasterContentSnapshotByBaseId(string baseId, SnapshotStatus status)
    {
        var master = dbContext.Masters.SingleOrDefault(x => x.BaseId == baseId);
        if (master == null)
        {
            logger.LogInformation("Can't find a matched result in database, key: {BaseID}.",
                baseId);
            return null;
        }

        await dbContext.Entry(master)
            .Collection(b => b.MasterContentSnapshots)
            .LoadAsync();

        var target = master.MasterContentSnapshots.Where(i => i.Status >= status)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefault();

        if (target == null)
        {
            logger.LogInformation("Can't find any content snapshot in database, key: {BaseID}.",
                baseId);
            return target;
        }

        return target;
    }

    private string? GetClientIp()
    {
        var ip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ip)) ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        return ip;
    }
}