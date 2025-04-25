using System.ComponentModel;
using System.Text.RegularExpressions;
using AE.PID.Core;
using AE.PID.Server.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AE.PID.Server.Apis;

public static partial class AppApi
{
    [GeneratedRegex("[\\d.]+")]
    private static partial Regex MyRegex();

    public static RouteGroupBuilder MapAppEndpoints(this RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("app", GetLatestVersionInfo)
            .WithTags("客户端")
            .WithDescription("根据更新通道获取客户端最新版本信息。");
        groupBuilder.MapGet("app/download/{id:int}", DownloadInstaller)
            .WithName(nameof(DownloadInstaller))
            .WithTags("客户端")
            .WithDescription("下载指定版本的安装包。");

        groupBuilder.MapPost("app/", UploadInstaller)
            .DisableAntiforgery()
            .WithTags("客户端")
            .WithDescription("上传新版本安装包并创建版本记录，上传的安装包将被分配在InternalTesting通道。");
        groupBuilder.MapPost("app/{id:int}/promote", PromoteVersion)
            .WithTags("客户端")
            .WithDescription("将版本升级到更高可见性的通道。每次应用只提升一级通道。");
        groupBuilder.MapPost("app/{id:int}/demote", DemoteVersion)
            .WithTags("客户端")
            .WithDescription("将版本降级到更低可见性的通道。每次应用只降低一级通道。");
        groupBuilder.MapPost("app/{id:int}/update-release-notes", UpdateReleaseNotes)
            .WithTags("客户端")
            .WithDescription("修改指定版本的发布说明。");
        groupBuilder.MapPost("app/{id:int}/update-file-hash", UpdateFileHash)
            .WithTags("客户端");

        groupBuilder.MapGet("help/file/{id:int}", ([Description("版本ID")] int id) =>
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "file", id.ToString());
                return TypedResults.PhysicalFile(filePath, "application/octet-stream");
            })
            .WithTags("客户端");

        return groupBuilder;
    }

    private static Results<Ok<AppVersionDto>, NoContent, ProblemHttpResult> GetLatestVersionInfo(HttpContext context,
        LinkGenerator linkGenerator,
        AppDbContext dbContext,
        [FromQuery] [Description("更新通道")] VersionChannel? channel = null)
    {
        var query = dbContext.AppVersions.AsQueryable();
        switch (channel)
        {
            case VersionChannel.InternalTesting:
                break;
            case VersionChannel.LimitedBeta:
                query = query.Where(v =>
                    v.Channel == VersionChannel.GeneralAvailability || v.Channel == VersionChannel.LimitedBeta);
                break;
            case VersionChannel.GeneralAvailability:
            case null:
                query = query.Where(v => v.Channel == VersionChannel.GeneralAvailability);
                break;
            default:
                return TypedResults.Problem("Cancel order failed to process.", statusCode: 500);
        }

        var version = query
            .OrderByDescending(v => v.Major)
            .ThenByDescending(v => v.Minor)
            .ThenByDescending(v => v.Build)
            .ThenByDescending(v => v.Revision)
            .FirstOrDefault();

        if (version == null) return TypedResults.NoContent();

        var url = linkGenerator.GetUriByName(context, nameof(DownloadInstaller),
                      new { id = version.Id }) ??
                  string.Empty;
        return TypedResults.Ok(MapToDto(version, url));
    }

    private static Results<NotFound, PhysicalFileHttpResult> DownloadInstaller(AppDbContext dbContext,
        [FromRoute] [Description("版本ID")] int id = 0)
    {
        var version = id == 0
            ? dbContext.AppVersions.Where(x => x.Channel == VersionChannel.GeneralAvailability)
                .OrderByDescending(v => v.Major)
                .ThenByDescending(v => v.Minor)
                .ThenByDescending(v => v.Build)
                .ThenByDescending(v => v.Revision)
                .FirstOrDefault()
            : dbContext.AppVersions.Find(id);

        if (version == null || !File.Exists(version.PhysicalFile))
            return TypedResults.NotFound(); // Or handle the case when the file is not found

        var fileName = Path.GetFileName(version.PhysicalFile);
        return TypedResults.PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(), version.PhysicalFile),
            "application/octet-stream", fileName, enableRangeProcessing: true);
    }

    private static Results<Ok<AppVersionDto>, ProblemHttpResult> UploadInstaller(HttpContext context,
        LinkGenerator linkGenerator, AppDbContext dbContext,
        [FromForm] UploadInstallerDto dto)
    {
        var filePath = Path.Combine(PathConstants.InstallerPath, dto.Installer.FileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            dto.Installer.CopyTo(stream);
        }

        // You can process the version and release note as needed
        var versionStr = MyRegex().Match(Path.GetFileNameWithoutExtension(dto.Installer.FileName)).Value;

        // check if already exist
        if (dbContext.AppVersions.Any(x => x.Version == versionStr))
            return TypedResults.Problem();

        var version = new AppVersion
        {
            Version = versionStr,
            ReleaseNotes = dto.ReleaseNotes,
            PhysicalFile = filePath,
            Hash = HashHelper.ComputeSHA256Hash(filePath),
            Channel = VersionChannel.InternalTesting
        };
        dbContext.AppVersions.Add(version);
        dbContext.SaveChanges();

        var url = linkGenerator.GetUriByName(context, nameof(DownloadInstaller),
                      new { id = version.Id }) ??
                  string.Empty;
        return TypedResults.Ok(MapToDto(version, url));
    }

    private static async Task<Results<Ok<AppVersionDto>, NotFound, ProblemHttpResult>> PromoteVersion(
        HttpContext context, LinkGenerator linkGenerator,
        AppDbContext dbContext, [FromRoute] [Description("版本ID")] int id)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            var version = await dbContext.AppVersions.FindAsync(id);
            if (version == null) return TypedResults.NotFound();

            // 更新channel
            switch (version.Channel)
            {
                case VersionChannel.InternalTesting:
                    version.Channel = VersionChannel.LimitedBeta;
                    break;
                case VersionChannel.LimitedBeta:
                    version.Channel = VersionChannel.GeneralAvailability;
                    break;
                case VersionChannel.GeneralAvailability:
                    break;
                default:
                    return TypedResults.Problem();
            }

            version.ModifiedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            var url = linkGenerator.GetUriByName(context, nameof(DownloadInstaller),
                          new { id = version.Id }) ??
                      string.Empty;
            return TypedResults.Ok(MapToDto(version, url));
        }
        catch
        {
            await transaction.RollbackAsync();
            return TypedResults.Problem();
        }
    }

    private static async Task<Results<Ok<AppVersionDto>, NotFound, ProblemHttpResult>> DemoteVersion(
        HttpContext context, LinkGenerator linkGenerator,
        AppDbContext dbContext, [FromRoute] [Description("版本ID")] int id)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            var version = await dbContext.AppVersions.FindAsync(id);
            if (version == null) return TypedResults.NotFound();

            // 更新channel
            switch (version.Channel)
            {
                case VersionChannel.InternalTesting:
                    break;
                case VersionChannel.LimitedBeta:
                    version.Channel = VersionChannel.InternalTesting;
                    break;
                case VersionChannel.GeneralAvailability:
                    version.Channel = VersionChannel.LimitedBeta;
                    break;
                default:
                    return TypedResults.Problem();
            }

            version.ModifiedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            var url = linkGenerator.GetUriByName(context, nameof(DownloadInstaller),
                          new { id = version.Id }) ??
                      string.Empty;
            return TypedResults.Ok(MapToDto(version, url));
        }
        catch
        {
            await transaction.RollbackAsync();
            return TypedResults.Problem();
        }
    }

    private static Results<Ok<AppVersionDto>, NotFound> UpdateReleaseNotes(HttpContext context,
        LinkGenerator linkGenerator, AppDbContext dbContext,
        [FromRoute] [Description("版本ID")] int id, [FromBody] [Description("发布说明")] string releaseNotes)
    {
        var version = dbContext.AppVersions.Find(id);

        if (version == null) return TypedResults.NotFound(); // Or handle the case when the file is not found

        version.ReleaseNotes = releaseNotes;
        version.ModifiedAt = DateTime.UtcNow;

        dbContext.AppVersions.Update(version);
        dbContext.SaveChanges();

        var url = linkGenerator.GetUriByName(context, nameof(DownloadInstaller),
                      new { id = version.Id }) ??
                  string.Empty;
        return TypedResults.Ok(MapToDto(version, url));
    }

    private static Results<Ok<AppVersionDto>, NotFound> UpdateFileHash(HttpContext context,
        LinkGenerator linkGenerator, AppDbContext dbContext, [FromRoute] [Description("版本ID")] int id)
    {
        var version = dbContext.AppVersions.Find(id);

        if (version == null) return TypedResults.NotFound(); // Or handle the case when the file is not found

        version.Hash = HashHelper.ComputeSHA256Hash(version.PhysicalFile);
        version.ModifiedAt = DateTime.UtcNow;

        dbContext.AppVersions.Update(version);
        dbContext.SaveChanges();

        var url = linkGenerator.GetUriByName(context, nameof(DownloadInstaller),
                      new { id = version.Id }) ??
                  string.Empty;
        return TypedResults.Ok(MapToDto(version, url));
    }

    private static AppVersionDto MapToDto(AppVersion version, string downloadUrl)
    {
        return new AppVersionDto
        {
            Version = version.Version,
            DownloadUrl = downloadUrl,
            FileHash = version.Hash,
            FileName = Path.GetFileName(version.PhysicalFile),
            ReleaseNotes = version.ReleaseNotes,
            Channel = version.Channel
        };
    }
}