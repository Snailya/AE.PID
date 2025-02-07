namespace AE.PID.Server.Interfaces;

public class MaterialRecommendation
{
    /// <summary>
    ///     推荐的物料Id
    /// </summary>
    public int MaterialId { get; set; }

    /// <summary>
    ///     推荐使用的算法
    /// </summary>
    public string Algorithm { get; set; }

    /// <summary>
    ///     推荐在排序层中的排名
    /// </summary>
    public int Rank { get; set; }
}