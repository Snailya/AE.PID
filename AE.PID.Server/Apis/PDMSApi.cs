using System.ComponentModel;
using AE.PID.Core;
using AE.PID.Core.DTOs;
using AE.PID.Server.Core;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AE.PID.Server.Apis;

public static class PDMSApi
{
    public static RouteGroupBuilder MapPDMSEndpoints(this RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("projects", GetProjects)
            .WithDescription("获取项目列表。")
            .WithTags("项目");
        groupBuilder.MapGet("projects/{id:int}", GetProjectById)
            .WithDescription("根据Id获取项目信息。")
            .WithTags("项目");

        groupBuilder.MapGet("categories", GetMaterialCategories)
            .WithDescription("获取物料分类。")
            .WithTags("物料");
        groupBuilder.MapGet("categories/map", GetMaterialCategoryMap)
            .WithDescription("获取物料分类和子类的映射关系。")
            .WithTags("物料");
        groupBuilder.MapGet("materials", GetMaterials)
            .WithDescription("获取物料。")
            .WithTags("物料");
        groupBuilder.MapGet("materials/{code}", GetMaterialsByCode)
            .WithDescription("根据编码获取物料。")
            .WithTags("物料");

        groupBuilder.MapGet("functions", GetFunctions)
            .WithDescription("获取功能位信息。")
            .WithTags("功能");
        groupBuilder.MapPost("functions", SynFunctions)
            .WithDescription("向PMDS同步功能组。")
            .WithTags("功能");;

        return groupBuilder;
    }

    private static async Task<Results<Ok<Paged<ProjectDto>>, NoContent, ProblemHttpResult>> GetProjects(
        HttpContext context,
        LinkGenerator linkGenerator,
        IProjectService projectService,
        [FromHeader(Name = "User-ID")] string userId,
        [FromQuery] string query = "",
        [FromQuery] int pageNo = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var projects = await projectService.GetPagedProjects(query, pageNo, pageSize, userId);
            if (projects == null) return TypedResults.NoContent();

            return TypedResults.Ok(projects);
        }
        catch (HttpRequestException e)
        {
            return TypedResults.Problem(e.Message);
        }
    }

    private static async Task<Results<Ok<ProjectDto>, NotFound, ProblemHttpResult>> GetProjectById(HttpContext context,
        LinkGenerator linkGenerator,
        IProjectService projectService,
        [FromHeader(Name = "User-ID")] string userId,
        int id)
    {
        try
        {
            var project = await projectService.GetProjectById(id, userId);
            if (project == null) return TypedResults.NotFound();

            return TypedResults.Ok(project);
        }
        catch (HttpRequestException e)
        {
            return TypedResults.Problem(e.Message);
        }
    }

    private static async Task<Results<Ok<Paged<MaterialDto>>, NoContent, ProblemHttpResult>> GetMaterials(
        HttpContext context,
        LinkGenerator linkGenerator,
        IMaterialService materialService,
        [FromHeader(Name = "User-ID")] string userId,
        [FromQuery] string? category = null, [FromQuery] string? s = null,
        [FromQuery] int pageNo = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var materials = await materialService.GetMaterialsAsync(userId, category, s, pageNo, pageSize);
            if (materials is null) return TypedResults.NoContent();

            return TypedResults.Ok(materials);
        }
        catch (HttpRequestException e)
        {
            return TypedResults.Problem(e.Message);
        }
    }

    private static async Task<Results<Ok<MaterialDto>, NotFound, ProblemHttpResult>> GetMaterialsByCode(
        HttpContext context,
        LinkGenerator linkGenerator,
        IMaterialService materialService,
        [FromHeader(Name = "User-ID")] string userId,
        [FromRoute] string code)
    {
        try
        {
            var material = await materialService.GetMaterialByCodeAsync(userId, code);
            if (material is null) return TypedResults.NotFound();

            return TypedResults.Ok(material);
        }
        catch (HttpRequestException e)
        {
            return TypedResults.Problem(e.Message);
        }
    }

    private static async Task<Results<Ok<IEnumerable<MaterialCategoryDto>>, ProblemHttpResult>> GetMaterialCategories(
        HttpContext context,
        LinkGenerator linkGenerator,
        IMaterialService materialService,
        [FromHeader(Name = "User-Id")] string userId,
        [FromQuery] string? name = null)
    {
        try
        {
            var categories = await materialService.GetCategories(userId, name);
            return TypedResults.Ok(categories);
        }
        catch (HttpRequestException e)
        {
            return TypedResults.Problem(e.Message);
        }
    }

    private static Results<Ok<Dictionary<string, string[]>>, ProblemHttpResult> GetMaterialCategoryMap(
        HttpContext context,
        LinkGenerator linkGenerator)
    {
        return TypedResults.Ok(DataDictionary.MaterialCategories);
    }

    private static async Task<Results<Ok<IEnumerable<FunctionDto>>, ProblemHttpResult>> GetFunctions(
        HttpContext context,
        LinkGenerator linkGenerator,
        IFunctionService functionService,
        [FromHeader(Name = "User-ID")] string userId,
        [FromQuery] string? projectId = null,
        [FromQuery] string? functionId = null)
    {
        try
        {
            if (projectId == null && functionId == null)
                return TypedResults.Ok(await GetStandardFunctionGroupsAsync(functionService, userId));

            if (projectId != null && functionId == null)
                return TypedResults.Ok(await GetProjectFunctionZonesAsync(functionService, userId, projectId));
            if (projectId != null && functionId != null)
                return TypedResults.Ok(
                    await GetProjectFunctionGroupsAsync(functionService, userId, projectId, functionId));

            return TypedResults.Problem();
        }
        catch (Exception e)
        {
            return TypedResults.Problem(e.Message);
        }
    }

    private static async Task<Results<Ok<string>, ProblemHttpResult>> SynFunctions(
        HttpContext context,
        LinkGenerator linkGenerator,
        IFunctionService functionService,
        [FromHeader(Name = "User-ID")] string userId,
        [FromHeader(Name = "UUID")] string uuid,
        [FromQuery] [Description("待同步的项目Id")] string projectId,
        [FromQuery] [Description("待同步的工艺区域Id")]
        string functionId,
        [FromBody] [Description("待同步的功能组信息")] List<FunctionDto> subFunctions)
    {
        try
        {
            var groups = await functionService.SynFunctions(userId, uuid, projectId, functionId, subFunctions);
            return TypedResults.Ok(groups);
        }
        catch (HttpRequestException e)
        {
            return TypedResults.Problem(e.Message);
        }
    }

    private static async Task<IEnumerable<FunctionDto>> GetProjectFunctionZonesAsync(IFunctionService functionService,
        string userId, string projectId)
    {
        var zones = await functionService.GetProjectFunctionZonesAsync(userId, projectId);
        return zones;
    }

    private static async Task<IEnumerable<FunctionDto>> GetStandardFunctionGroupsAsync(IFunctionService functionService,
        string userId)
    {
        var groups = await functionService.GetStandardFunctionGroupsAsync(userId);
        return groups;
    }

    private static async Task<IEnumerable<FunctionDto>> GetProjectFunctionGroupsAsync(IFunctionService functionService,
        string userId, string projectId, string functionId)
    {
        var groups = await functionService.GetProjectFunctionGroupsAsync(userId, projectId, functionId);
        return groups;
    }
}