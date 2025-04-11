using AE.PID.Core;
using AE.PID.Server.Core;
using AE.PID.Server.Data.Recommendation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AE.PID.Server.Apis;

public static class RecommendationApi
{
    public static RouteGroupBuilder MapRecommendationEndpoints(this RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("recommendations/materials", GetMaterialSuggestion)
            .WithDescription("获取PDMS请求时需要使用的Header信息，用于手动触发PDMS请求")
            .WithTags("推荐");

        groupBuilder.MapPost("recommendations/materials", FeedbackMaterialSelections)
            .WithDescription("获取PDMS请求时需要使用的Header信息，用于手动触发PDMS请求")
            .WithTags("推荐");
        return groupBuilder;
    }

    private static Results<Ok<int>, ProblemHttpResult> FeedbackMaterialSelections(
        HttpContext context,
        IRecommendService recommendService,
        [FromHeader(Name = "User-ID")] string userId,
        [FromBody] UserMaterialSelectionFeedbackDto[] feedbacks)
    {
        try
        {
            var count = recommendService.SaveFeedbackMaterialSelections(userId, feedbacks);
            return TypedResults.Ok(count);
        }
        catch (Exception e)
        {
            return TypedResults.Problem(e.Message);
        }
    }

    private static async Task<Results<Ok<MaterialRecommendationCollectionDto>, ProblemHttpResult>>
        GetMaterialSuggestion(
            IRecommendService recommendService, IMaterialService materialService,
            [FromHeader(Name = "User-ID")] string userId,
            [FromQuery] int? projectId = null, [FromQuery] string? functionZone = null,
            [FromQuery] string? functionGroup = null,
            [FromQuery] string? functionElement = null, [FromQuery] string? materialLocationType = null)
    {
        // 当前
        var context = new MaterialLocationContext
        {
            ProjectId = projectId,
            FunctionZone = functionZone ?? string.Empty,
            FunctionGroup = functionGroup ?? string.Empty,
            FunctionElement = functionElement ?? string.Empty,
            MaterialLocationType = materialLocationType ?? string.Empty
        };

        var recommendationResult = recommendService.GetMaterialRecommendations(userId, context);
        var items = (await Task.WhenAll(recommendationResult.Recommendations
                .Select(async x => await ToMaterialRecommendationResultItemDto(materialService, x))))
            .Where(x => x != null).Cast<MaterialRecommendationDto>().ToList();

        var dto = new MaterialRecommendationCollectionDto
        {
            Id = recommendationResult.Id,
            Items = items
        };

        return TypedResults.Ok(dto);
    }

    private static async Task<MaterialRecommendationDto?> ToMaterialRecommendationResultItemDto(
        IMaterialService materialService,
        MaterialRecommendation source)
    {
        var material = await materialService.GetMaterialByIdAsync("6470", source.MaterialId);
        if (material == null) return null; // 如果在数据库中找不到推荐的对象，虽然这个不应该发生，则忽略这条推荐，因为本来就是推荐，缺少数据也不影响。

        return new MaterialRecommendationDto
        {
            Id = source.Id,
            Rank = source.Rank,
            Material = material,
            Algorithm = source.Algorithm
        };
    }
}