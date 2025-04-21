using System.IO.Packaging;
using AE.PID.Core;
using AE.PID.Server.Data;

namespace AE.PID.Server;

public class VisioDocumentService(ILogger<VisioDocumentService> logger, AppDbContext dbContext)
    : IVisioDocumentService
{
    private readonly VisioDocumentProcessor _processor = new();

    public async Task<string> UpdateDocumentStencils(string? clientIp, IFormFile file, MasterDto[]? items,
        SnapshotStatus status)
    {
        // 2025.02.03： 由于System.IO.Packaging的Package.Open方法必须读取本地的文件，所以这里需要对传入的字节流进行本地缓存。这样做也有利于事后回溯。
        // 此处存储时没有使用文件原本的名称，而是使用时间戳，原因是如果API请求来自于Windows系统，并且使用的是FullPath，通过Path.GetFileName方法获取文件名的时候，还必须替换其中的“\”字符，否则在Linux中由于分隔符的不同，获取结果仍为完整路径，引发后面报错。
        var filePath = await FileHelper.SaveToTmpFile(file, $"{DateTime.Now:yyyyMMddHHmmssfff}.vsdx");

        logger.LogInformation("Received file {FileName} from ip address {IP} cached at {Path}.", file.FileName,
            clientIp, filePath);

        // 2025.02.05: 获得需要更新的Master信息
        using var visioPackage = Package.Open(filePath, FileMode.Open, FileAccess.ReadWrite);

        logger.LogInformation("Starting update of {FileName}...", filePath);

        // update styles
        _processor.UpdateStyles(visioPackage);

        // do update
        var documentMasters = VisioDocumentProcessor.GetDocumentMasters(visioPackage);

        var index = 0;
        var total = items?.Length ?? documentMasters.Length;

        foreach (var source in documentMasters)
        {
            // 2025.02.05: 如果制定了更新列表，需要检查当前的master是不是在更新列表之中，如果不在的话，则跳过
            if (items != null && items.All(x => x.UniqueId != source.UniqueId)) continue;

            logger.LogInformation("Processing {MasterName} ({Index} / {Total})... ", source.Name, ++index,
                total);

            var target = await GetLatestMaster(source.BaseId, status);

            if (target is not null)
                VisioDocumentProcessor.UpdateMaster(visioPackage, source.UniqueId, target);
        }

        // 添加一个重新计算公式的事件，如果没有的话
        XmlHelper.RecalculateDocument(visioPackage);

        visioPackage.Close();

        logger.LogInformation("Finished update of {FileName}", filePath);

        return filePath;
    }


    private async Task<MasterContentSnapshot?> GetLatestMaster(string baseId, SnapshotStatus status)
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
}