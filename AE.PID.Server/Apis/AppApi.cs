using System.Text.RegularExpressions;
using AE.PID.Core;
using AE.PID.Server.Data;
using AE.PID.Server.DTOs;
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
            .WithTags("客户端");
        groupBuilder.MapGet("app/download/{id:int}", DownloadInstaller)
            .WithName(nameof(DownloadInstaller))
            .WithTags("客户端");

        groupBuilder.MapPost("app/", UploadInstaller)
            .DisableAntiforgery()
            .WithTags("客户端");
        groupBuilder.MapPost("app/{id:int}/promote", PromoteVersion)
            .WithTags("客户端");
        groupBuilder.MapPost("app/{id:int}/demote", DemoteVersion)
            .WithTags("客户端");
        groupBuilder.MapPost("app/{id:int}/update-release-notes", UpdateReleaseNotes)
            .WithTags("客户端");
        groupBuilder.MapPost("app/{id:int}/update-file-hash", UpdateFileHash)
            .WithTags("客户端");

        groupBuilder.MapGet("help/file/{versionId:int}", (int versionId) =>
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "file", versionId.ToString());
                return TypedResults.PhysicalFile(filePath, "application/octet-stream");
            })
            .WithTags("客户端");
        
        return groupBuilder;
    }



    private static Results<Ok<AppVersionDto>, NoContent, ProblemHttpResult> GetLatestVersionInfo(HttpContext context,
        LinkGenerator linkGenerator,
        AppDbContext dbContext,
        [FromQuery] VersionChannel? channel = null)
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
        [FromRoute] int id = 0)
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
        AppDbContext dbContext, [FromRoute] int id)
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
        AppDbContext dbContext, [FromRoute] int id)
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
        [FromRoute] int id, [FromBody] string releaseNotes)
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
        LinkGenerator linkGenerator, AppDbContext dbContext, [FromRoute] int id)
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