using System.ComponentModel.DataAnnotations;
using AE.PID.Core.Models;

namespace AE.PID.Server.Data.Recommendation;

/// <summary>
///     推荐结果
/// </summary>
public class MaterialRecommendationCollection : EntityBase
{
    /// <summary>
    ///     用户Id。
    /// </summary>
    [MaxLength(8)]
    public string UserId { get; set; } = string.Empty;

    #region -- Navigation Properties --

    /// <summary>
    ///     产生的推荐结果
    /// </summary>
    public ICollection<MaterialRecommendation> Recommendations { get; set; } = [];

    /// <summary>
    ///     生成推荐结果时使用费的上下文, 多对多
    /// </summary>
    [Required]
    public required MaterialLocationContext Context { get; set; } = null!;

    #endregion
}