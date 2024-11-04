using System.Collections.Generic;

namespace AE.PID.Core.DTOs;

public class MaterialRecommendationCollectionDto
{
    /// <summary>
    ///     此次推荐结果的Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     推荐结果的具体内容。
    /// </summary>
    public List<MaterialRecommendationDto> Items { get; set; }
}