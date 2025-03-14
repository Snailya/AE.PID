namespace AE.PID.Core;

public class MaterialRecommendationDto
{
    /// <summary>
    ///     推荐项的Id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     推荐的物料
    /// </summary>
    public MaterialDto Material { get; set; } = null!;

    /// <summary>
    ///     生成该推荐物料使用的算法
    /// </summary>
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>
    ///     该推荐项的排名
    /// </summary>
    public int Rank { get; set; }
}