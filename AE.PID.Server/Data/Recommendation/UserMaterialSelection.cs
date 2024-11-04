using System.ComponentModel.DataAnnotations;
using AE.PID.Core.Models;
using AE.PID.Server.Models;

namespace AE.PID.Server.Data.Recommendation;

/// <summary>
///     用来记录用户选择的物料记录
/// </summary>
public class UserMaterialSelection : EntityBase
{
    /// <summary>
    ///     用户的Id，用户的个人偏好会影响物料的选择结果
    /// </summary>
    [MaxLength(8)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    ///     选择的物料Id
    /// </summary>
    [Required]
    public int MaterialId { get; set; }
    
    /// <summary>
    ///     用户选择物料时执行的上下文。多对多
    /// </summary>
    public MaterialLocationContext Context { get; set; }
}