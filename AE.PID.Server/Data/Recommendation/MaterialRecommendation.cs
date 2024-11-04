using System.ComponentModel.DataAnnotations;

namespace AE.PID.Server.Data.Recommendation;

/// <summary>
///     推荐的物料项
/// </summary>
public class MaterialRecommendation : EntityBase
{
    /// <summary>
    ///     推荐的物料Id。
    /// </summary>
    public int MaterialId { get; set; }

    /// <summary>
    ///     该推荐结果在排序层中的顺序。
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    ///     产生该推荐结果使用的模型。
    /// </summary>
    [MaxLength(512)]
    public string Algorithm { get; set; } = string.Empty;
}