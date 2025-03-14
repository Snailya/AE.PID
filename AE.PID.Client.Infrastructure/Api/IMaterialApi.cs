using System.Collections.Generic;
using System.Threading.Tasks;
using AE.PID.Core;
using Refit;

namespace AE.PID.Visio.Shared;

public interface IMaterialApi
{
    [Get("/api/v3/categories")]
    Task<IEnumerable<MaterialCategoryDto>> GetCategoriesAsync();

    [Get("/api/v3/categories/map")]
    Task<Dictionary<string, string[]>> GetCategoriesMapAsync();

    [Get("/api/v3/materials")]
    Task<Paged<MaterialDto>> GetMaterialsAsync([Query] int? category, [Query] string s, [Query] int pageNo,
        [Query] int pageSize);

    [Get("/api/v3/materials/{materialNo}")]
    Task<MaterialDto> GetMaterialByCodeAsync(string materialNo);

    [Get("/api/v3/recommendations/materials")]
    Task<MaterialRecommendationCollectionDto> GetRecommendedMaterialsAsync([Query] int? projectId = null,
        [Query] string? functionZone = null,
        [Query] string? functionGroup = null, [Query] string? functionElement = null,
        [Query] string? materialLocationType = null);

    [Post("/api/v3/recommendations/materials")]
    Task<int> FeedbackMaterialSelectionsAsync(UserMaterialSelectionFeedbackDto[] feedbacks);
}