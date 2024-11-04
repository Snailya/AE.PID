using System.ComponentModel.DataAnnotations;

namespace AE.PID.Server.Data.Recommendation;

/// <summary>
///     此类用于记录用户对于推荐结果的反馈
/// </summary>
public class MaterialRecommendationCollectionFeedback : EntityBase
{
    /// <summary>
    ///     用户的信息。
    /// </summary>
    [MaxLength(8)]
    public string UserId { get; set; } = string.Empty;

    #region -- Navigation Properties --

    /// <summary>
    ///     与此次反馈关联的推荐集
    /// </summary>
    public int CollectionId { get; set; }

    #endregion

    /// <summary>
    ///     用户实际选择的结果的Id
    /// </summary>
    public int? SelectedRecommendationId { get; set; }
}