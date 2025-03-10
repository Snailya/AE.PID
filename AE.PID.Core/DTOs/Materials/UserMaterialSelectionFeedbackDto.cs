namespace AE.PID.Core.DTOs;

/// <summary>
///     用户对于推荐物料的选择结果，用作模型的训练输入。
/// </summary>
public class UserMaterialSelectionFeedbackDto
{
    /// <summary>
    ///     用户选择的物料Id
    /// </summary>
    public int MaterialId { get; set; }

    /// <summary>
    ///     物料位点的上下文信息
    /// </summary>
    public MaterialLocationContext MaterialLocationContext { get; set; }

    /// <summary>
    ///     用户开始选择时，可能还没有推荐模型，所以允许为空
    /// </summary>
    public int? RecommendationCollectionId { get; set; }

    /// <summary>
    ///     用户可能根本没有选择推荐结果中的对象，所以允许为空
    /// </summary>
    public int? SelectedRecommendationId { get; set; }
}