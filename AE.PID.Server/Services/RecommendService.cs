using AE.PID.Core;
using AE.PID.Server.Data;
using AE.PID.Server.Data.Recommendation;
using MaterialRecommendation = AE.PID.Server.Data.Recommendation.MaterialRecommendation;

namespace AE.PID.Server;

public class RecommendService(ILogger<RecommendService> logger, AppDbContext dbContext) : IRecommendService
{
    public MaterialRecommendationCollection GetMaterialRecommendations(string userContext,
        MaterialLocationContext locationContext)
    {
        // 首先检查有没有今天已经生成的result
        var result = dbContext.MaterialRecommendationCollections.SingleOrDefault(x =>
            x.UserId == userContext && x.Context == locationContext &&
            x.CreatedAt.Date == DateTime.Now.Date);
        if (result != null) return result;

        // 如果没有已经生成的数据，则重新生成模型
        var userPreferred = GetUserPreferred(userContext, locationContext, 3);
        var globalPopular = GetPopular(locationContext, 3);
        var items = userPreferred.Concat(globalPopular).GroupBy(x => x.MaterialId).Select((x, index) =>
            new MaterialRecommendation
            {
                CreatedAt = DateTime.Now,
                MaterialId = x.Key,
                Rank = index + 1,
                Algorithm = string.Join(",", x.Select(i => i.Algorithm))
            }).ToList();

        result = new MaterialRecommendationCollection
        {
            CreatedAt = DateTime.Now,
            UserId = userContext,
            Recommendations = items,
            Context = locationContext
        };

        dbContext.MaterialRecommendationCollections.Add(result);
        dbContext.SaveChanges();

        return result;
    }

    public int SaveFeedbackMaterialSelections(string userId, UserMaterialSelectionFeedbackDto[] feedbackDtos)
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

        return count;
    }

    #region -- 多路召回 --

    /// <summary>
    ///     用户流行
    /// </summary>
    /// <param name="userContext"></param>
    /// <param name="locationContext"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    private IEnumerable<(int MaterialId, string Algorithm)> GetUserPreferred(string userContext,
        MaterialLocationContext locationContext,
        int count)
    {
        // first check if there is records from the same user
        return dbContext.UserMaterialSelections
            .Where(x => x.UserId == userContext &&
                        x.Context.MaterialLocationType == locationContext.MaterialLocationType)
            .GroupBy(x => x.MaterialId).OrderByDescending(x => x.Count()).Take(count)
            .Select(x => new ValueTuple<int, string>(x.Key, "User Preferred")).ToList();
    }


    /// <summary>
    ///     热门物品
    /// </summary>
    /// <param name="locationContext"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    private IEnumerable<(int MaterialId, string Algorithm)> GetPopular(MaterialLocationContext locationContext,
        int count)
    {
        return dbContext.UserMaterialSelections
            .Where(x => x.Context.MaterialLocationType == locationContext.MaterialLocationType)
            .GroupBy(x => x.MaterialId).OrderByDescending(x => x.Count()).Take(count)
            .Select(x => new ValueTuple<int, string>(x.Key, "User Preferred")).ToList();
    }

    /// <summary>
    ///     上下文预测
    /// </summary>
    /// <param name="userContext"></param>
    /// <param name="locationContext"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    private IEnumerable<string> GetContextPrediction(string userContext, MaterialLocationContext locationContext,
        int count)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     基于用户的协同过滤
    /// </summary>
    /// <param name="userContext"></param>
    /// <param name="locationContext"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private IEnumerable<string> GetUserCF(string userContext, MaterialLocationContext locationContext, int count)
    {
        throw new NotImplementedException();
    }

    #endregion
}