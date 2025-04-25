using System.ComponentModel;
using System.Text.Json;
using AE.PID.Core;
using AE.PID.Server.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AE.PID.Server.Apis;

public static class VisioDocumentApi
{
    public static RouteGroupBuilder MapVisioDocumentEndpoints(this RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("masters/snapshots", GetLatestMasters)
            .WithDescription("获取所有的模具。用于判断客户端的Visio文档是否需要更新。")
            .WithTags("Visio文档");

        groupBuilder.MapPost("documents/update", UpdateDocumentStencil)
            .DisableAntiforgery()
            .WithDescription("更新文档模具。")
            .WithTags("Visio文档");

        return groupBuilder;
    }

    private static async Task<Results<Ok<IEnumerable<MasterSnapshotDto>>,
            ProblemHttpResult>>
        GetLatestMasters(
            HttpContext context,
            LinkGenerator linkGenerator, AppDbContext dbContext,
            [FromQuery] [Description("快照状态")] SnapshotStatus status = SnapshotStatus.Published,
            [FromQuery] int? mode = 0)
    {
        // 第一阶段：获取每个 Master 最新的有效 Snapshot
        var masterQuery = dbContext.Masters
            .Select(master => new
            {
                Master = master,
                LatestSnapshot = master.MasterContentSnapshots
                    .Where(s => s.Status >= status)
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefault()
            })
            .Where(x => x.LatestSnapshot != null);

        // 第二阶段：批量获取所有需要的 UniqueIdHistory
        var masterIds = await masterQuery
            .Select(x => x.Master.Id)
            .Distinct()
            .ToListAsync();

        // 使用字典存储历史记录，Key: MasterId, Value: UniqueId数组
        var historyDict = await dbContext.MasterContentSnapshots
            .Where(s => masterIds.Contains(s.MasterId))
            .GroupBy(s => s.MasterId)
            .Select(g => new
            {
                MasterId = g.Key,
                History = g.Select(s => s.UniqueId).ToArray()
            })
            .ToDictionaryAsync(k => k.MasterId, v => v.History);

        // 最终查询组装
        var finalQuery = masterQuery
            .AsNoTracking()
            .Select(x => new
            {
                x.Master,
                x.LatestSnapshot
            });

        var results = await finalQuery.ToListAsync();

        return mode switch
        {
            // 标准模式
            0 => TypedResults.Ok(results.Select(x => new MasterSnapshotDto
            {
                Name = x.Master.Name,
                BaseId = x.Master.BaseId,
                UniqueId = x.LatestSnapshot?.UniqueId ?? string.Empty,
                UniqueIdHistory = historyDict.TryGetValue(x.Master.Id, out var history)
                    ? history
                    : []
            })),
            _ => TypedResults.Problem()
        };
    }

    private static async Task<Results<PhysicalFileHttpResult, ProblemHttpResult>> UpdateDocumentStencil(
        HttpContext context,
        LinkGenerator linkGenerator, AppDbContext dbContext,
        IVisioDocumentService visioDocumentService,
        IFormFile file, [FromForm] string? data = null,
        [FromQuery] [Description("快照状态")] SnapshotStatus status = SnapshotStatus.Published)
    {
        //  2025.02.06: 由于Refit不支持不咋结构数组作为Form的一部分，此处将原来的MasterDto[]? 修改为string?，然后再反序列化。
        var items = data != null ? JsonSerializer.Deserialize<MasterDto[]>(data) : null;

        // do the update
        try
        {
            var filePath =
                await visioDocumentService.UpdateDocumentStencils(context.GetClientIp(), file, items, status);

            // return a physical file
            return TypedResults.PhysicalFile(filePath, "application/octet-stream");
        }
        catch (Exception e)
        {
            return TypedResults.Problem(
                e.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Algorithm Error"
            );
        }
    }
}