using System.Text;
using System.Text.Json;
using AE.PID.Server.Core;
using AE.PID.Server.Data;
using AE.PID.Server.DTOs;
using AE.PID.Server.Extensions;
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
            .WithDescription("获取PDMS请求时需要使用的Header信息，用于手动触发PDMS请求")
            .WithTags("调试");
        groupBuilder.MapGet("stencils", GetStencils)
            .WithTags("调试");

        groupBuilder.MapPost("documents/update-by-file", UpdateDocumentStencil)
            .DisableAntiforgery()
            .WithTags("调试");

        groupBuilder.MapGet("materials/file", GetMaterialsAsFile)
            .WithDescription("获取物料数据的Json文件。")
            .WithTags("调试");

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
            return TypedResults.Problem(e.Message);
        }
    }

    private static Results<Ok<Stencil>, Ok<DbSet<Stencil>>, NotFound> GetStencils(HttpContext context,
        LinkGenerator linkGenerator, AppDbContext dbContext, [FromQuery] int? id = null)
    {
        if (id == null) return TypedResults.Ok(dbContext.Stencils);

        var stencil = dbContext.Stencils.Find(id);
        if (stencil == null) return TypedResults.NotFound();
        return TypedResults.Ok(stencil);
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
            var stream = new MemoryStream(byteArray);

            return TypedResults.Stream(stream, "application/json",
                $"category={category}&no={pageNo}&size={pageSize}.json");
        }
        catch (BadHttpRequestException e)
        {
            return TypedResults.Problem(e.Message);
        }
    }
}