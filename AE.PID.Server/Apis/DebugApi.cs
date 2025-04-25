using System.Text;
using System.Text.Json;
using AE.PID.Server.Core;
using AE.PID.Server.Data;
using AE.PID.Server.DTOs;
using AE.PID.Server.PDMS;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AE.PID.Server.Apis;

public static class DebugApi
{
    public static RouteGroupBuilder MapDebugEndpoints(this RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("pdms", PDMSApiResolver.CreateHeader)
            .WithTags("调试")
            .WithSummary("PDMS请求头")
            .WithDescription("获取PDMS请求时需要使用的Header信息，用于手动触发PDMS请求。");

        groupBuilder.MapGet("stencils", GetStencils)
            .WithTags("调试")
            .WithSummary("模具信息")
            .WithDescription("获取模具及其最新的快照。");

        groupBuilder.MapPost("documents/update-by-file", UpdateDocumentStencil)
            .DisableAntiforgery()
            .WithTags("调试")
            .WithSummary("文档更新")
            .WithDescription("更新上传的文档的文档模具。");

        groupBuilder.MapGet("materials/file", GetMaterialsAsFile)
            .WithTags("调试")
            .WithSummary("物料文件")
            .WithDescription("获取物料数据的Json文件。");

        return groupBuilder;
    }


    private static async Task<Results<PhysicalFileHttpResult, ProblemHttpResult>> UpdateDocumentStencil(
        HttpContext context, IVisioDocumentService visioDocumentService, DocumentMasterUpdateRequestDto requestDto,
        [FromQuery] SnapshotStatus status = SnapshotStatus.Published)
    {
        try
        {
            var filePath = await visioDocumentService.UpdateDocumentStencils(context.GetClientIp(), requestDto.File,
                requestDto.Items, status);

            // 2025.02.03: add default filename
            return TypedResults.PhysicalFile(filePath, "application/octet-stream", requestDto.File.FileName);
        }
        catch (Exception e)
        {
            return TypedResults.Problem(e.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<Results<Ok<StencilAuditDto>, Ok<List<StencilAuditDto>>, NotFound>> GetStencils(
        HttpContext context,
        LinkGenerator linkGenerator, AppDbContext dbContext, [FromQuery] int? id = null)
    {
        var query = dbContext.Stencils
            .Where(x => id == null || x.Id == id)
            .Select(x => new StencilAuditDto
            {
                Id = x.Id,
                Name = x.Name,
                CreatedAt = x.CreatedAt,
                ModifiedAt = x.ModifiedAt,
                LatestSnapshots = x.StencilSnapshots
                    .OrderByDescending(i => i.Id)
                    .Take(3)
                    .Select(i => new StencilSnapshotAuditDto
                    {
                        Id = i.Id,
                        Description = i.Description,
                        CreatedAt = i.CreatedAt,
                        ModifiedAt = i.ModifiedAt,
                        Status = i.Status
                    })
            })
            .AsNoTracking();

        if (id == null)
        {
            var collection = await query.ToListAsync();
            return TypedResults.Ok(collection);
        }

        var item = await query.FirstOrDefaultAsync();
        return item != null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<FileStreamHttpResult, NoContent, ProblemHttpResult>> GetMaterialsAsFile(
        HttpContext context,
        LinkGenerator linkGenerator, AppDbContext dbContext,
        IMaterialService materialService,
        [FromHeader(Name = "User-ID")] string userId,
        [FromQuery] string? category = null, [FromQuery] string? s = null,
        [FromQuery] int pageNo = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var materials = await materialService.GetFlattenMaterialsAsync(userId, category, s, pageNo, pageSize);

            if (materials == null) return TypedResults.NoContent();

            var json = JsonSerializer.Serialize(materials);
            var byteArray = Encoding.UTF8.GetBytes(json);
            using var stream = new MemoryStream(byteArray);

            return TypedResults.Stream(stream, "application/json",
                $"category={category}&no={pageNo}&size={pageSize}.json");
        }
        catch (BadHttpRequestException e)
        {
            return TypedResults.Problem(e.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
}