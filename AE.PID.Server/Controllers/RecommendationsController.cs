using AE.PID.Core.DTOs;
using AE.PID.Core.Models;
using AE.PID.Server.Data;
using AE.PID.Server.Data.Recommendation;
using AE.PID.Server.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MaterialRecommendation = AE.PID.Server.Data.Recommendation.MaterialRecommendation;

namespace AE.PID.Server.Controllers;

[ApiController]
[Route("api/v{apiVersion:apiVersion}/[controller]")]
[ApiVersion(3)]
public class RecommendationsController(
    ILogger<RecommendationsController> logger,
    AppDbContext dbContext,
    IRecommendService recommendService,
    IMaterialService materialService,
    LinkGenerator linkGenerator) : ControllerBase
{
    [HttpPost("materials")]
    public IActionResult FeedbackMaterialSelections([FromHeader(Name = "User-ID")] string userId,
        [FromBody] UserMaterialSelectionFeedbackDto[] feedbackDtos)
    {
        try
        {
            foreach (var feedbackDto in feedbackDtos)
            {
                // 首先处理用户记录
                var userMaterialSelection = new UserMaterialSelection
                {
                    CreatedAt = DateTime.Now,
                    Context = feedbackDto.MaterialLocationContext,
                    UserId = userId,
                    MaterialId = feedbackDto.MaterialId
                };
                dbContext.UserMaterialSelections.AddRange(userMaterialSelection);

                // 如果有建议，处理建议
                if (feedbackDto.RecommendationCollectionId != null)
                {
                    var recommendationResult =
                        dbContext.MaterialRecommendationCollections.Find(feedbackDto.RecommendationCollectionId.Value);
                    if (recommendationResult == null)
                    {
                        logger.LogWarning("Unable to find the recommendation result with id: {Id}, skipped.",
                            feedbackDto.RecommendationCollectionId.Value);
                        continue;
                    }

                    dbContext.Entry(recommendationResult).Collection(x => x.Recommendations).Load();
                    var feedback = new MaterialRecommendationCollectionFeedback
                    {
                        CreatedAt = DateTime.Now,
                        UserId = userId,
                        CollectionId = recommendationResult.Id,
                        SelectedRecommendationId = feedbackDto.SelectedRecommendationId
                    };
                    dbContext.MaterialRecommendationCollectionFeedbacks.Add(feedback);
                }
            }

            var count = dbContext.SaveChanges();

            logger.LogInformation("{Count} selection records added.", count);

            return Accepted(count);
        }
        catch (DbUpdateException e)
        {
            return UnprocessableEntity(e.InnerException);
        }
    }

    [HttpGet("materials")]
    public async Task<IActionResult> GetMaterialSuggestion([FromHeader(Name = "User-ID")] string userId,
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
                .Select(async x => await ToMaterialRecommendationResultItemDto(x))))
            .Where(x => x != null).ToList();

        var dto = new MaterialRecommendationCollectionDto
        {
            Id = recommendationResult.Id,
            Items = items
        };

        return Ok(dto);
    }

    private async Task<MaterialRecommendationDto?> ToMaterialRecommendationResultItemDto(
        MaterialRecommendation source)
    {
        var material = await materialService.GetMaterialByIdAsync("6470", source.MaterialId);
        if (material == null)
        {
            logger.LogWarning("Unable to find the material with id: {Id} in database", source.MaterialId);
            return null; // 如果在数据库中找不到推荐的对象，虽然这个不应该发生，则忽略这条推荐，因为本来就是推荐，缺少数据也不影响。
        }

        return new MaterialRecommendationDto
        {
            Id = source.Id,
            Rank = source.Rank,
            Material = material,
            Algorithm = source.Algorithm
        };
    }
}